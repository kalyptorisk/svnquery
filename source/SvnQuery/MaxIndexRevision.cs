#region Apache License 2.0

// Copyright 2008 Christian Rodemeyer
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace SvnQuery
{
    public static class MaxIndexRevision
    {
        public static int Get(string index)
        {
            IndexSearcher s = null;
            try
            {
                s = new IndexSearcher(index);
                return Get(s.Reader);
            }
            finally
            {
                if (s != null) s.Close();
            }
        }

        public static int Get(IndexReader reader)
        {
            string max_rev_first = "00000001";
            TermEnum te = reader.Terms(new Term("rev_first", max_rev_first));
            while (true)
            {
                Term t = te.Term();
                if (t == null || t.Field() != "rev_first") break;
                max_rev_first = t.Text();
                te.Next();
            }

            string max_rev_last = max_rev_first;
            te.SkipTo(new Term("rev_last", max_rev_last));
            while (true)
            {
                Term t = te.Term();
                if (t == null || t.Field() != "rev_last" || t.Field() != RevisionFilter.HeadString) break;
                max_rev_last = t.Text();
                te.Next();
            }

            return Math.Max(int.Parse(max_rev_first), int.Parse(max_rev_last));
        }
    }
}