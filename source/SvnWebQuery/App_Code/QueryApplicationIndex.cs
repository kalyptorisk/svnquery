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

using System.Collections.Generic;
using System.Web;

namespace App_Code
{
    /// <summary>
    /// This class provides the select methods needed by an ObjectDataSource to implement databinding
    /// </summary>
    public static class QueryApplicationIndex
    {
        static Index Index
        {
            get { return (Index) HttpContext.Current.Application["Index"]; }
        }

        public static string Name
        {
            get { return Index.Name; }
        }

        public static string Uri
        {
            get { return Index.Uri; }
        }

        public static QueryResult Query(string query, string revFirst, string revLast)
        {
            return Index.Query(query, revFirst, revLast);
        }

        public static Hit QueryId(string id)
        {
            return Index.Query(id);
        }
        
        public static IEnumerable<Hit> Select(string query, string revFirst, string revLast, int maximumRows,
                                              int startRowIndex)
        {
            QueryResult r = Query(query, revFirst, revLast);
            for (int i = startRowIndex; i < r.HitCount && i < startRowIndex + maximumRows; ++i)
            {
                yield return r[i];
            }
        }

        public static int SelectCount(string query, string revFirst, string revLast)
        {
            return Query(query, revFirst, revLast).HitCount;
        }

    }
}