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
using System.Linq;
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

        public HitList Hits { get; private set; }
    }
}