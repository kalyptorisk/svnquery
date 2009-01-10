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
using Lucene.Net.Index;
using Lucene.Net.Search;
using SvnQuery;

namespace App_Code
{
    public class Index
    {
        readonly string index;
        readonly Timer timer;
        readonly Dictionary<string, CachedQueryResult> cache = new Dictionary<string, CachedQueryResult>();

        volatile IndexSearcher indexSearcher;
        volatile int indexRevision;

        public Index(string index)
        {
            this.index = index;

            UpdateIndexSearcher();

            timer = new Timer(90000); // Check for index updates and cleaning caches
            timer.Enabled = true;
            timer.Elapsed += delegate { CleanupCache(); };
        }

        ~Index()
        {
            timer.Dispose();
            if (indexSearcher != null) indexSearcher.Close();
        }

        // creates and warms up a new IndexSearcher if necessary
        bool UpdateIndexSearcher()
        {
            if (indexSearcher == null || !indexSearcher.Reader.IsCurrent())
            {
                IndexSearcher searcher = new IndexSearcher(index);
                int revision = MaxIndexRevision.Get(searcher.Reader);
                searcher.Search(new TermQuery(new Term("path", "warmup")));
                if (indexSearcher == null)
                {
                    indexSearcher = searcher;
                    indexRevision = revision;
                }
                else
                {
                    IndexSearcher oldSearcher = indexSearcher;
                    lock (indexSearcher)
                    {
                        indexSearcher = searcher;
                        indexRevision = revision;
                    }
                    oldSearcher.Close();
                }
                return true;
            }
            return false;
        }

        public QueryResult Query(string query, string revFirst, string revLast)
        {
            CachedQueryResult result;
            string key = query + revFirst + revLast;
            lock (cache) cache.TryGetValue(key, out result);
            if (result == null)
            {
                result = new CachedQueryResult(ExecuteQuery(query, revFirst, revLast));
                lock (cache) cache[key] = result;
            }
            result.LastAccess = DateTime.Now;
            return result.Result;
        }

        QueryResult ExecuteQuery(string query, string revFirst, string revLast)
        {
            Stopwatch sw = Stopwatch.StartNew();

            IndexSearcher searcher;
            int revision;

            lock (indexSearcher)
            {
                searcher = indexSearcher;
                revision = indexRevision;
            }

            Parser p = new Parser(searcher.Reader);

            // Optimizations:
            // don't use the revision filter for head queries or all queries

            Hits hits;
            Query q = p.Parse(query);
            if (revFirst == RevisionFilter.HeadString) // Head Query
            {
                var headQuery = new BooleanQuery();
                headQuery.Add(q, BooleanClause.Occur.MUST);
                headQuery.Add(new TermQuery(new Term(FieldName.RevisionLast, RevisionFilter.HeadString)),
                              BooleanClause.Occur.MUST);
                hits = searcher.Search(headQuery /*, new Sort("id")*/); // if we need to sort
            }
            else if (revFirst == RevisionFilter.AllString) // All Query
            {
                hits = searcher.Search(q);
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
                cache.Clear();
            }
            else
            {
                DateTime now = DateTime.Now;
                List<string> too_old_entries = new List<string>();
                lock (cache)
                {
                    foreach (var pair in cache)
                    {
                        if ((now - pair.Value.LastAccess).TotalMinutes > 10)
                            too_old_entries.Add(pair.Key);
                    }
                    foreach (var s in too_old_entries)
                    {
                        cache.Remove(s);
                    }
                }
            }
        }
    }
}