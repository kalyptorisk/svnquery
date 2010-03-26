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
using SvnQuery;

namespace SvnWebQuery.Code
{
    public class HitViewModel
    {
        readonly Hit _hit;
        readonly string _path;
        readonly string _link;

        public HitViewModel(Hit hit)
        {
            _hit = hit;
            string id = hit.Id;
            _path = id.Split('@')[0];
            _link = "View.aspx?id=" +  Uri.EscapeDataString(id);            
        }

        public string Link
        {
            get { return _link; }
        }

        public string Path
        {
            get { return _path; }
        }

        public string Folder
        {
            get { return _path.Substring(0, _path.LastIndexOf('/')); }
        }

        public string File
        {
            get { return _path.Substring(_path.LastIndexOf('/') + 1); }
        }

        public string RevFirst
        {
            get { return _hit.RevisionFirst; }
        }

        public string RevLast
        {
            get { return _hit.RevisionLast; }
        }

        public int Revision
        {
            get { return _hit.Revision; }
        }

        /// <summary>
        /// The maximum size as extracted from the packed size
        /// </summary>
        public int MaxSize
        {
            get { return _hit.SizeInBytes; }
        }

        public string Size
        {
            get { return _hit.Size; }
        }

        public string Author
        {
            get { return _hit.Author; }
        }

        public string LastModification
        {
            get { return _hit.LastModification.ToString("g"); }
        }

        public string Summary
        {
            get
            {
                string summary = Author + ": &nbsp;" + LastModification;
                summary += "&nbsp; - &nbsp;" + RevFirst + ":" + RevLast;

                string size = Size;
                if (!string.IsNullOrEmpty(size))
                    summary += "&nbsp; - &nbsp;" + size;

                return summary;
            }
        }
    }
}