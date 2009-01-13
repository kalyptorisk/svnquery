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
using Lucene.Net.Search;

namespace SvnQuery
{
    public static class MaxIndexRevision
    {
        public static int Get(string index)
        {
            IndexSearcher s = null;
            try
            {
                s = new IndexSearcher(index);
                return Get(s.Reader);
            }
            finally
            {
                if (s != null) s.Close();
            }
        }

        public static int Get(IndexReader reader)
        {
            TermDocs td = reader.TermDocs(new Term(FieldName.Id, DocumentId.IndexRevision));
            if (!td.Next()) return 0;
            return int.Parse(reader.Document(td.Doc()).Get(DocumentId.IndexRevision));
        }
    }
}