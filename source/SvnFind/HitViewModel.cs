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
using SvnQuery;

namespace SvnFind
{
    public class HitViewModel
    {
        readonly Hit _hit;

        public HitViewModel(Hit hit)
        {
            _hit = hit;
        }

        public string Path
        {
            get { return _hit.Path; }
        }

        public string File
        {
            get { return _hit.File; }
        }

        public string Folder
        {
            get { return _hit.Folder; }
        }

        public string Author
        {
            get { return _hit.Author; }
        }

        public string LastModified
        {
            get { return _hit.LastModification.ToShortDateString() + " " + _hit.LastModification.ToShortTimeString(); }
        }

        public int Revision
        {
            get { return _hit.Revision; }
        }

        public string RevisionRange
        {
            get { return _hit.RevisionFirst + ":" + _hit.RevisionLast; }
        }

        public string Size
        {
            get { return _hit.Size; }
        }

        public int SizeInBytes
        {
            get { return _hit.SizeInBytes; }
        }

        public override string ToString()
        {
            return _hit.ToString();
        }
    }
}