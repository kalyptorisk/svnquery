

using System.Diagnostics;
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
        readonly string _pathToIndex;
        IndexSearcher _searcher;
        readonly object _sync = new object();

        public Index(string pathToIndex)
        {
            _pathToIndex = pathToIndex;
        }
     
        public IndexProperties QueryProperties()
        {
            return new IndexProperties(GetSearcher().Reader);
        }

        public Result Query(string query)
        {
            return Query(query, RevisionFilter.HeadString, RevisionFilter.HeadString);
        }

        public Result Query(string query, string revFirst, string revLast)
        {
            Stopwatch sw = Stopwatch.StartNew();

            IndexSearcher searcher = GetSearcher();
            Parser p = new Parser(searcher.Reader);
            Query q = p.Parse(query);

            Hits hits;
            if (IsSingleRevision || revFirst == RevisionFilter.AllString) // All Query
            {
                hits = searcher.Search(q);
            }
            else if (revFirst == RevisionFilter.HeadString) // Head Query
            {
                var headQuery = new BooleanQuery();
                headQuery.Add(q, BooleanClause.Occur.MUST);
                headQuery.Add(new TermQuery(new Term(FieldName.RevisionLast, RevisionFilter.HeadString)), BooleanClause.Occur.MUST);
                hits = searcher.Search(headQuery);
            }
            else // Revision Query
            {
                hits = searcher.Search(q, new RevisionFilter(int.Parse(revFirst), int.Parse(revLast)));
            }

            IndexProperties properties = new IndexProperties(searcher.Reader);
            return new Result(sw.Elapsed, properties, hits);
        }

        IndexSearcher GetSearcher()
        {
            lock (_sync)
            {
                if (_searcher == null || !_searcher.Reader.IsCurrent())
                {
                    if (_searcher != null) _searcher.Close();
                    _searcher = new IndexSearcher(_pathToIndex);
                }                
            }
            return _searcher;
        }

        public bool IsSingleRevision { get; private set; }
    }
}