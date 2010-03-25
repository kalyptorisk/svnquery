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

        public int Revision {get; private set; }
        public string RepositoryName { get; private set; }
        public string RepositoryLocalUri { get; private set; }
        public string RepositoryExternalUri { get; private set; }
        public Credentials RepositoryCredentials { get; private set; }
        public bool SingleRevision { get; private set; }

        /// <summary>number of indexed documents</summary>
        public int TotalCount { get; private set; } 
    }
}