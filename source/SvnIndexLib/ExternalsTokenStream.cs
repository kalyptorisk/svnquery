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

using System.IO;
using Lucene.Net.Analysis;
namespace SvnIndex
{
    public class ExternalsTokenStream: TokenStream
    {
        const int MaxTokenLength = 250;

        TextReader reader;
        string line;
        int offset;
        bool eol;

        public ExternalsTokenStream()
        {}

        public ExternalsTokenStream(string externals)
        {       
            SetReader(new StringReader(externals));
        }

        public bool Reset(string externals)
        {
            if (string.IsNullOrEmpty(externals)) return false;
            SetReader(new StringReader(externals));
            return true;
        }

        public bool IsEmpty
        {
            get {return reader == null;}
        }

        protected void SetReader(TextReader r)
        {
            reader = r;
            line = null;
            offset = 0;
            eol = true;
        }

        public override Token Next(Token token)
        {
            token.Clear();

            if (reader == null) return null;

            int length = 0;
            char[] buffer = token.TermBuffer();
            if (buffer.Length < MaxTokenLength) buffer = token.ResizeTermBuffer(MaxTokenLength);

            while (true)
            {
                if (line == null)
                {
                    line = reader.ReadLine();
                    while (line == "") line = reader.ReadLine();
                    if (line == null)
                    {
                        reader = null;
                        return null;
                    }
                    line = line.Trim();
                    offset = 0;                                     
                }

                if (offset >= line.Length)
                {
                    line = null;
                    if (length > 0) break;
                    eol = true;
                }

                if (eol)
                {
                    buffer[length++] = ':';
                    eol = false;
                    break;
                }

                char c = char.ToLowerInvariant(line[offset++]);
                if (c == '\\') c = '/';
                if (c == '/')
                {
                    if (length > 0) break; 
                    continue;
                }
                if (char.IsWhiteSpace(c))
                {                    
                    offset = line.Length; // skip to end of line
                    if (length > 0) break;
                    continue;
                }               

                buffer[length++] = c;
                if (length >= MaxTokenLength) break;
            }
            token.SetTermLength(length);
            return token;
        }

       
    }
}