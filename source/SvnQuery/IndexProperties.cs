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
using Lucene.Net.Index;
using SvnQuery.Lucene;
using SvnQuery.Svn;

namespace SvnQuery
{
    public class IndexProperties
    {
        internal IndexProperties(IndexReader reader)
        {
            Revision = IndexProperty.GetRevision(reader);
            RepositoryName = IndexProperty.GetRepositoryName(reader);
            RepositoryLocalUri = IndexProperty.GetRepositoryLocalUri(reader);
            RepositoryExternalUri = IndexProperty.GetRepositoryExternalUri(reader);
            RepositoryCredentials = IndexProperty.GetRepositoryCredentials(reader);
            SingleRevision = IndexProperty.GetSingleRevision(reader);
            TotalCount = reader.MaxDoc();
        }

        public int Revision { get; private set; }
        public string RepositoryName { get; private set; }
        public string RepositoryLocalUri { get; private set; }
        public string RepositoryExternalUri { get; private set; }
        public Credentials RepositoryCredentials { get; private set; }
        public bool SingleRevision { get; private set; }

        /// <summary>number of indexed documents</summary>
        public int TotalCount { get; private set; }
    }
}