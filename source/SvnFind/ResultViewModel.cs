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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using SvnFind.Diagnostics;
using SvnQuery;
using SvnQuery.Svn;

namespace SvnFind
{
    public class ResultViewModel
    {
        readonly Result _result;

        public ResultViewModel(Result result)
        {
            _result = result;
            Svn = new SharpSvnApi(_result.Index.RepositoryExternalUri);
        }

        public ISvnApi Svn {get; private set;}

        public string HitCount
        {
            get { return _result.Hits.Count.ToString(); }
        }

        public string HitsFor
        {
            get { return _result.Hits.Count == 1 ? "hit for" : "hits for"; }
        }

        public string Query
        {
            get { return _result.Query; }
        }

        public string Statistics
        {
            get
            {
                const string fmt = "{0} files searched in {1}ms. Revision is {2}";
                return string.Format(fmt, _result.Index.TotalCount, (int) _result.SearchTime.TotalMilliseconds, _result.Index.Revision);
            }
        }

        public IEnumerable<HitViewModel> Hits
        {
            get
            {
                foreach (Hit hit in _result.Hits)
                {
                    yield return new HitViewModel(hit);
                }
            }
        }

    }
}