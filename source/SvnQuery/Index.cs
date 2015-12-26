#region Apache License 2.0

// Copyright 2008-2010 Christian Rodemeyer
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
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Search;
using SvnQuery.Lucene;

namespace SvnQuery
{
    /// <summary>
    /// Thread safe!
    /// </summary>
    public class Index
    {
        IndexSearcher _searcher;
        readonly object _sync = new object();

        public Index(string pathToIndex)
        {
            Path = pathToIndex;
            var props = QueryProperties();
            Name = props.RepositoryName;
            IsSingleRevision = props.SingleRevision;
        }

        public string Path { get; private set; }

        public string Name { get; private set; }

        public IndexProperties QueryProperties()
        {
            return new IndexProperties(GetSearcher().Reader);
        }

        public Hit GetHitById(string id)
        {
            IndexSearcher s = GetSearcher();
            Hits hits = s.Search(new TermQuery(new Term(FieldName.Id, id)));
            return hits.Length() == 1 ? new Hit(hits.Doc(0), null) : null;
        }

        public Result Query(string query)
        {
            return Query(query, Revision.HeadString, Revision.HeadString, null);
        }

        public Result Query(string query, string revFirst, string revLast)
        {
            return Query(query, revFirst, revLast, null);
        }

        public Result Query(string query, string revFirst, string revLast, Highlight highlight)
        {
            Stopwatch sw = Stopwatch.StartNew();

            IndexSearcher searcher = GetSearcher();
            var p = new Parser(searcher.Reader);
            Query q = p.Parse(query);

            Hits hits;
            if (IsSingleRevision || revFirst == Revision.AllString) // All Query
            {
                hits = searcher.Search(q);
            }
            else if (revFirst == Revision.HeadString) // Head Query
            {
                var headQuery = new BooleanQuery();
                headQuery.Add(q, BooleanClause.Occur.MUST);
                headQuery.Add(new TermQuery(new Term(FieldName.RevisionLast, Revision.HeadString)), BooleanClause.Occur.MUST);
                hits = searcher.Search(headQuery);
            }
            else // Revision Query
            {
                hits = searcher.Search(q, new RevisionFilter(int.Parse(revFirst), int.Parse(revLast)));
            }

            var properties = new IndexProperties(searcher.Reader);
            return new Result(query, sw.Elapsed, properties, hits,
                highlight != null ? highlight.GetFragments(q, hits) : null);
        }

        IndexSearcher GetSearcher()
        {
            lock (_sync)
            {
                if (_searcher == null || !_searcher.Reader.IsCurrent())
                {
                    if (_searcher != null) _searcher.Close();
                    _searcher = new IndexSearcher(Path);
                }
            }
            return _searcher;
        }

        public bool IsSingleRevision { get; private set; }

        public override string ToString()
        {
            return Name;
        }
    }
}