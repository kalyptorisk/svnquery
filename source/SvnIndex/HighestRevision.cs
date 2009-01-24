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

        public IndexReader Reader; 

        public int Get(string path)
        {
            int revision;
            lock (highest) highest.TryGetValue(path, out revision);
            if (revision != 0) return revision;

            if (Reader == null) return 0;
            int max = 0;
            path += "@";
            TermEnum t = Reader.Terms(new Term(FieldName.Id, path));
            while (t.Term() != null && t.Term().Text().StartsWith(path))
            {
                int r = int.Parse(t.Term().Text().Substring(path.Length));
                if (r > max)
                {
                    max = r;
                }
                t.Next();
            }  
            t.Close();

            var td = Reader.TermDocs(new Term(FieldName.Id, path + max));
            if (td.Next())
            {
                var doc = Reader.Document(td.Doc());
                revision = int.Parse(doc.Get(FieldName.RevisionLast));
                td.Close();                
            }
            return revision;
        }

        /// <summary>
        /// Sets the highest revision for a path. Returns true if revision was already set
        /// </summary>
        /// <param name="path"></param>
        /// <param name="revision"></param>
        /// <returns></returns>
        public bool Set(string path, int revision)
        {
            lock (highest)
            {
                int existing;
                if (highest.TryGetValue(path, out existing) && existing == revision) return false;
                if (revision < existing) throw new InvalidOperationException("revision order is badly wrong");
                highest[path] = revision;
                return true;
            }
        }

    }
}