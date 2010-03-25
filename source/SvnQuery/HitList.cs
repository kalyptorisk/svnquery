using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Search;

namespace SvnQuery
{
    public class HitList: IEnumerable<Hit>
    {
        internal HitList(Hits hits)
        {}

        public IEnumerator<Hit> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }
      
        public Hit this[int index]
        {
            get { throw new NotImplementedException(); }
        }
    }

       //public HitViewModel this[int i]
       // {
       //     get { return new HitViewModel(_luceneHits.Doc(i)); }
       // }

       // public IEnumerator<HitViewModel> GetEnumerator()
       // {
       //     for (int i = 0; i < HitCount; ++i)
       //     {
       //         yield return this[i];
       //     }
       // }
}