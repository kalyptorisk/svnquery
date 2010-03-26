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
using System.Linq;
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
        static DateTime _lastCacheCleanup = DateTime.Now;

        static ApplicationIndex()
        {
            Index = new Index(WebConfigurationManager.AppSettings["IndexPath"]);
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

        public static Result Query(string query, string revFirst, string revLast)
        {
            CleanupCache();
            CachedResult result;
            string key = query + revFirst + revLast;
            lock (Cache) Cache.TryGetValue(key, out result);
            if (result == null)
            {
                result = new CachedResult(Index.Query(query, revFirst, revLast));
                lock (Cache) Cache[key] = result;
            }
            result.LastAccess = DateTime.Now;
            return result.Result;
        }

        // removes too old entries from the cache
        static void CleanupCache()
        {
            DateTime now = DateTime.Now;

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
            Result r = Query(query, revFirst, revLast);
            for (int i = startRowIndex; i < r.Hits.Count && i < startRowIndex + maximumRows; ++i)
            {
                yield return new HitViewModel(r.Hits[i]);
            }
        }

        public static int SelectCount(string query, string revFirst, string revLast)
        {
            return Query(query, revFirst, revLast).Hits.Count;
        }

        #endregion
    }
}