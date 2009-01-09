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

using System.Web.Configuration;
using SvnQuery;

namespace App_Code
{
    public class Hit
    {
        static readonly string repositoryUrl;

        static Hit()
        {
           repositoryUrl = WebConfigurationManager.AppSettings["RepositoryUrl"];
        }

        readonly Lucene.Net.Documents.Document doc;
        readonly string path;

        public Hit(Lucene.Net.Documents.Document doc)
        {
            this.doc = doc;
            path = doc.Get("id").Split(':')[0];
        }

        public string Path
        {
            get { return path; }
        }

        public string Url
        {
            get { return repositoryUrl + path; }
        }

        public string Folder
        {
            get { return path.Substring(0, path.LastIndexOf('/')); }
        }

        public string File
        {
            get { return path.Substring(path.LastIndexOf('/') + 1); }
        }

        static string NiceRev(string rev)
        {
            return rev == RevisionFilter.HeadString ? "head" : rev.TrimStart('0');
        }

        public string RevFirst
        {
            get { return NiceRev(doc.Get(FieldName.RevisionFirst)); }
        }

        public string RevLast
        {
            get { return NiceRev(doc.Get(FieldName.RevisionLast)); }
        }

        public string Size
        {
            get
            {
                string size = doc.Get(FieldName.Size);
                return string.IsNullOrEmpty(size) ? "" : PackedSizeConverter.FromSortableStringToString(size);
            }
        }

        public string Author
        {
            get { return doc.Get(FieldName.Author); }
        }

        public string LastModification
        {
            get { return doc.Get(FieldName.Timestamp); }
        }

        public string Summary
        {
            get
            {
                string summary = Author + ": &nbsp;" + LastModification;
                summary += "&nbsp; - &nbsp" + RevFirst + ":" + RevLast;

                string size = Size;
                if (!string.IsNullOrEmpty(size))
                    summary += "&nbsp; - &nbsp" + size;

                return summary;
            }
        }



    }

}