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
using SvnQuery;
using SvnQuery.Lucene;

namespace SvnIndex
{
    /// <summary>
    /// Threadsafe lookup and updates of highest active revision.
    /// Should be used only by the Analyze Threads
    /// </summary>
    public class HighestRevision
    {
        readonly Dictionary<string, int> _highest = new Dictionary<string, int>();

        public IndexReader Reader
        {
            get { return _reader; }
            set
            {
                _reader = value;
                _highest.Clear();
            }
        }
        IndexReader _reader;

        public int Get(string path)
        {
            int revision;
            lock (_highest)
            {
                if (_highest.TryGetValue(path, out revision)) return revision;
            }

            if (Reader == null) return 0;
            path += "@";
            TermEnum t = Reader.Terms(new Term(FieldName.Id, path));
            int doc = -1;
            while (t.Term() != null && t.Term().Text().StartsWith(path))
            {
                int r = int.Parse(t.Term().Text().Substring(path.Length));
                if (r > revision)
                {
                    revision = r;
                    TermDocs d = Reader.TermDocs(t.Term());
                    d.Next();
                    doc = d.Doc();                    
                }
                t.Next();
            }              
            t.Close();
            if (revision != 0 && Reader.Document(doc).Get(FieldName.RevisionLast) != Revision.HeadString)
                return 0;
            return revision;
        }

        /// <summary>
        /// Sets the highest revision for a path. Returns true if revision was already set
        /// </summary>
        /// <param name="path"></param>
        /// <param name="revision">if 0 then the path is currently deleted</param>
        /// <returns></returns>
        public bool Set(string path, int revision)
        {
            lock (_highest)
            {
                int existing;
                if (_highest.TryGetValue(path, out existing) && existing == revision) return false;
                //if (revision < existing) throw new InvalidOperationException("revision order is badly wrong");
                _highest[path] = revision;
                return true;
            }
        }

    }
}