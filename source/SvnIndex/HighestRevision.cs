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

using System.Collections.Generic;
using Lucene.Net.Index;

namespace SvnQuery
{
    /// <summary>
    /// Threadsafe lookup and updates of highest revision
    /// </summary>
    public class HighestRevision
    {
        readonly Dictionary<string, int> highest = new Dictionary<string, int>();
        
        public IndexReader Reader {get; set;}

        public int Get(string path)
        {
            int revision;
            if (highest.TryGetValue(path, out revision)) return revision;

            TermEnum t = Reader.Terms(new Term(FieldName.Id, path));
            int max = 0;
            while (t.Next())
            {
                int r = int.Parse(t.Term().Text().Substring(path.Length));
                if (r > max)
                {
                    max = r;
                }
            }
            t.Close();

            var td = Reader.TermDocs(new Term(FieldName.Id, path + "@" + max));
            var doc = Reader.Document(td.Doc());
            td.Close();
            return int.Parse(doc.Get(FieldName.RevisionLast));
        }

        public void Set(string path, int revision)
        {
            highest[path] = revision;           
        }


    }
}