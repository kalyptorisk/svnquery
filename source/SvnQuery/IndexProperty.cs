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
using Lucene.Net.Index;
using Lucene.Net.Documents;

namespace SvnQuery
{
    public static class IndexProperty
    {
        const string IdField = "$Property";
        const string ValueField = "$Value";
        const string RevisionProperty = "Revision";
        const string RepositoryIdProperty = "RepositoryId";

        /// <summary>
        /// returns a term that uniquely identifies a document containing an index property
        /// </summary>        
        static Term GetPropertyId(string property)
        {
            return new Term(IdField, property);
        }

        static string GetProperty(IndexReader reader, string property)
        {
            TermDocs td = reader.TermDocs(GetPropertyId(property));
            if (!td.Next()) return null;
            return reader.Document(td.Doc()).Get(ValueField);
        }

        static void UpdateProperty(IndexWriter writer, string property, string value)
        {
            writer.DeleteDocuments(GetPropertyId(property));
            var doc = new Document();
            doc.Add(new Field(IdField, property, Field.Store.NO, Field.Index.UN_TOKENIZED));
            doc.Add(new Field(ValueField, value, Field.Store.YES, Field.Index.NO));
            writer.AddDocument(doc);
        }

        public static int GetRevision(IndexReader reader)
        {
            return int.Parse(GetProperty(reader, RevisionProperty));
        }

        public static Guid GetRepositoryId(IndexReader reader)
        {
            return new Guid(GetProperty(reader, RepositoryIdProperty));
        }

        public static void UpdateRevision(IndexWriter writer, int revision)
        {
            UpdateProperty(writer, RevisionProperty, revision.ToString());
        }

        public static void UpdateRepositoryId(IndexWriter writer, Guid repositoryId)
        {
            UpdateProperty(writer, RepositoryIdProperty, repositoryId.ToString());            
        }
    }
}