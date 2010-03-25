using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SvnQuery
{
    public class HitList: IEnumerable<Hit2>
    {
        public IEnumerator<Hit2> GetEnumerator()
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
      
        public Hit2 this[int index]
        {
            get { throw new NotImplementedException(); }
        }
    }
}