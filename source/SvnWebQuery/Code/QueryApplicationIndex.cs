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
using System.Linq;
using System.Web.Configuration;

namespace SvnWebQuery.Code
{
    /// <summary>
    /// This class provides the select methods needed by an ObjectDataSource to implement databinding
    /// </summary>
    public static class QueryApplicationIndex
    {
        static Index _index;

        static Index Index
        {
            get
            {
                if (_index == null)
                {
                    _index = new Index(WebConfigurationManager.AppSettings["IndexPath"]);
                }
                return _index;
            }
        }

        public static string Name
        {
            get { return Index.Name; }
        }

        /// <summary>
        /// used to access the repository from this application
        /// </summary>
        public static string LocalUri
        {
            get { return Index.LocalUri; }
        }

        public static bool IsSingleRevision
        {
            get { return Index.IsSingleRevision; }
        }

        /// <summary>
        /// used to access the repository from this application
        /// </summary>
        public static string ExternalUri
        {
            get { return Index.ExternalUri; }
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