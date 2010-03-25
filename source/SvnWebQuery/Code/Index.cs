#region Apache License 2.0

// Copyright 2008-2009 Christian Rodemeyer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using SvnQuery.Lucene;
using SvnQuery.Svn;

namespace SvnWebQuery.Code
{
    public class Index
    {
        readonly string _index;
        readonly Timer _timer;
        readonly Dictionary<string, CachedQueryResult> _cache = new Dictionary<string, CachedQueryResult>();
        readonly object _sync = new object();

        IndexSearcher _indexSearcher;
        int _repositoryRevision;

        public Index(string index)
        {
            _index = index;

            UpdateIndexSearcher();
            IsSingleRevision = IndexProperty.GetSingleRevision(_indexSearcher.Reader);

            _timer = new Timer(90000); // Check for index updates and cleaning caches
            _timer.Enabled = true;
            _timer.Elapsed += delegate { CleanupCache(); };
        }

        ~Index()
        {
            if (_timer != null) _timer.Dispose();
            if (_indexSearcher != null) _indexSearcher.Close();
        }

        public string Name { get; private set; }

        public string LocalUri { get; private set; }

        public string ExternalUri { get; private set; }

        public Credentials Credentials { get; private set; }

        public bool IsSingleRevision {get; private set;}

        // creates and warms up a new IndexSearcher if necessary
        bool UpdateIndexSearcher()
        {
            if (_indexSearcher != null && _indexSearcher.Reader.IsCurrent())
                return false;
            
            IndexSearcher searcher = new IndexSearcher(_index);
            searcher.Search(new TermQuery(new Term("path", "warmup")));

            IndexReader reader = searcher.Reader;
            int indexRevision = IndexProperty.GetRevision(reader);
            string localUri = IndexProperty.GetRepositoryLocalUri(reader);
            string externalUri = IndexProperty.GetRepositoryExternalUri(reader);
            string indexName = IndexProperty.GetRepositoryName(reader) ?? localUri.Split('/').Last();
            Credentials credentials = IndexProperty.GetRepositoryCredentials(reader);

            lock (_sync)
            {
                if (_indexSearcher != null) _indexSearcher.Close();
                _indexSearcher = searcher;
                _repositoryRevision = indexRevision;
                Name = indexName;
                LocalUri = localUri;
                ExternalUri = externalUri;
                Credentials = credentials; 
            }
            return true;
        }

        public QueryResult Query(string query, string revFirst, string revLast)
        {
            CachedQueryResult result;
            string key = query + revFirst + revLast;
            lock (_cache) _cache.TryGetValue(key, out result);
            if (result == null)
            {
                result = new CachedQueryResult(ExecuteQuery(query, revFirst, revLast));
                lock (_cache) _cache[key] = result;
            }
            result.LastAccess = DateTime.Now;
            return result.Result;
        }

        public HitViewModel Query(string id)
        {
            IndexSearcher s = _indexSearcher;
            Hits h = s.Search(new TermQuery(new Term(FieldName.Id, id)));
            return h.Length() == 1 ? new HitViewModel(h.Doc(0)) : null; 
        }

        QueryResult ExecuteQuery(string query, string revFirst, string revLast)
        {
            Stopwatch sw = Stopwatch.StartNew();

            IndexSearcher searcher;
            int revision;

            lock (_sync)
            {
                searcher = _indexSearcher;
                revision = _repositoryRevision;
            }

            Parser p = new Parser(searcher.Reader);

            // Optimizations:
            // don't use the revision filter for head queries or all queries

            Hits hits;
            Lucene.Net.Search.Query q = p.Parse(query);
            if (IsSingleRevision || revFirst == RevisionFilter.AllString) // All Query
            {
                hits = searcher.Search(q);
            }
            else if (revFirst == RevisionFilter.HeadString) // Head Query
            {
                var headQuery = new BooleanQuery();
                headQuery.Add(q, BooleanClause.Occur.MUST);
                headQuery.Add(new TermQuery(new Term(FieldName.RevisionLast, RevisionFilter.HeadString)),
                              BooleanClause.Occur.MUST);
                hits = searcher.Search(headQuery /*, new Sort("id")*/); // if we need to sort
            }
            else // Revision Query
            {
                hits = searcher.Search(q, new RevisionFilter(int.Parse(revFirst), int.Parse(revLast)));
            }

            return new QueryResult(sw, revision, searcher.MaxDoc(), hits);
        }

        class CachedQueryResult
        {
            public CachedQueryResult(QueryResult r)
            {
                Result = r;
            }

            public readonly QueryResult Result;
            public DateTime LastAccess;
        }

        // removes too old entries from the cache
        void CleanupCache()
        {
            if (UpdateIndexSearcher()) // true if index got updated
            {
                _cache.Clear();
            }
            else
            {
                DateTime now = DateTime.Now;
                List<string> tooOldEntries = new List<string>();
                lock (_cache)
                {
                    foreach (var pair in _cache)
                    {
                        if ((now - pair.Value.LastAccess).TotalMinutes > 10)
                            tooOldEntries.Add(pair.Key);
                    }
                    foreach (var s in tooOldEntries)
                    {
                        _cache.Remove(s);
                    }
                }
            }
        }
    }
}