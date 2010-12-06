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
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using SvnQuery;
using SvnQuery.Svn;

namespace SvnWebQuery.Code
{
    /// <summary>
    /// Provides the select methods needed by an ObjectDataSource to implement databinding.
    /// Implements caching of results.
    /// </summary>
    public static class ApplicationIndex
    {
        static readonly Index Index;
        static readonly Dictionary<string, CachedResult> Cache = new Dictionary<string, CachedResult>();
        static readonly TimeSpan CacheCleanupIntervall = TimeSpan.FromSeconds(90);
        static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        static DateTime _lastCacheCleanup = DateTime.UtcNow;

        static ApplicationIndex()
        {
            string cacheDurationText = WebConfigurationManager.AppSettings["CacheDuration"];
            if (!String.IsNullOrEmpty(cacheDurationText))
                CacheDuration = TimeSpan.Parse(WebConfigurationManager.AppSettings["CacheDuration"]);

            Index = OpenIndex();
            var props = Index.QueryProperties(); 
            Name = props.RepositoryName;
            IsSingleRevision = props.SingleRevision;
            SvnApi = new SharpSvnApi(props.RepositoryLocalUri, props.RepositoryCredentials.User, props.RepositoryCredentials.Password);
        }
        
        public static string Name
        {
            get; private set; 
        }

        public static bool IsSingleRevision
        {
            get; private set;
        }
   
        public static ISvnApi SvnApi
        {
            get; private set;
        }

        public static HitViewModel GetHitById(string id)
        {
            return new HitViewModel(Index.GetHitById(id));
        }

        public static Result Query(string query, string revFirst, string revLast, bool useCache)
        {
            CleanupCache();
            CachedResult result = null;
            string key = query + "|" + revFirst + "|" + revLast;
            if (useCache)
                lock (Cache) Cache.TryGetValue(key, out result);
            if (result == null)
            {
                result = new CachedResult(Index.Query(query, revFirst, revLast));
                lock (Cache) Cache[key] = result;
            }
            result.LastAccess = DateTime.UtcNow;
            return result.Result;
        }

        static Index OpenIndex()
        {
            string indexPath = WebConfigurationManager.AppSettings["IndexPath"];
            string indexParentPath = WebConfigurationManager.AppSettings["IndexParentPath"];

            if (!String.IsNullOrEmpty(indexPath) && !String.IsNullOrEmpty(indexParentPath))
                throw new ApplicationException("Invalid web configuration: Both IndexPath and IndexParentPath are set but only one may be set.");

            if (String.IsNullOrEmpty(indexPath) && String.IsNullOrEmpty(indexParentPath))
                throw new ApplicationException("Invalid web configuration: Neither IndexPath nor IndexParentPath is set.");

            if (!String.IsNullOrEmpty(indexParentPath))
            {
                // build index path by combining parent path and name of virtual web
                indexPath = Path.Combine(indexParentPath, Path.GetFileName(HttpRuntime.AppDomainAppVirtualPath.TrimEnd('/')));
            }

            Index index = new Index(indexPath);
            return index;
        }

        // removes too old entries from the cache
        static void CleanupCache()
        {
            DateTime now = DateTime.UtcNow;

            if (now - _lastCacheCleanup < CacheCleanupIntervall)
                return;

            _lastCacheCleanup = now;
            lock (Cache)
            {
                var tooOld = Cache.Where(p => now - p.Value.LastAccess > CacheDuration).Select(p => p.Key);
                foreach (var s in new List<string>(tooOld))
                {
                    Cache.Remove(s);
                }
            }
        }

        class CachedResult
        {
            public CachedResult(Result result)
            {
                Result = result;
            }

            public readonly Result Result;
            public DateTime LastAccess;
        }

        #region ASP.NET databinding

        public static IEnumerable<HitViewModel> Select(string query, string revFirst, string revLast, int maximumRows, int startRowIndex)
        {
            Result r = Query(query, revFirst, revLast, true);
            var hits = r.Hits.OrderBy(hit => hit.Path, StringComparer.InvariantCultureIgnoreCase);
            return hits.Skip(startRowIndex).Take(maximumRows).Select(hit => new HitViewModel(hit));
        }

        public static int SelectCount(string query, string revFirst, string revLast)
        {
            return Query(query, revFirst, revLast, true).Hits.Count;
        }

        #endregion
    }
}