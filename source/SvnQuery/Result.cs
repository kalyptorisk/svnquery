using System;
using System.Linq;
using System.Text;
using Lucene.Net.Search;

namespace SvnQuery
{
    public class Result
    {
        internal Result(TimeSpan searchTime, IndexProperties indexProperties, Hits hits)
        {
            SearchTime = searchTime;
            Index = indexProperties;
            Hits = new HitList(hits);
        }

        public TimeSpan SearchTime { get; private set; }

        public IndexProperties Index { get; private set; }

        public HitList Hits { get; private set;}
    }
}
