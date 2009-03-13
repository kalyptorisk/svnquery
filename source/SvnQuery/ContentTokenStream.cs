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

using System.IO;
using Lucene.Net.Analysis;

namespace SvnQuery
{
    /// <summary>
    /// Analyses identifier tokens 
    /// </summary>
    /// <remarks>
    /// No need to override Reset() or Close()
    /// </remarks>
    public class ContentTokenStream : TokenStream
    {
        const int MinTokenLength = 1;
        const int MaxTokenLength = 80;

        TextReader reader;
        string line;
        int offset;
        readonly bool includeWildcards;

        public ContentTokenStream()
        {}

        public ContentTokenStream(string content, bool includeWildcards)
        {
            this.includeWildcards = includeWildcards;
            SetReader(new StringReader(content));
        }

        public bool SetText(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            SetReader(new StringReader(content));
            return true;
        }

        public bool IsEmpty
        {
            get { return reader == null; }
        }

        protected void SetReader(TextReader r)
        {
            reader = r;
            line = "";
            offset = 0;
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
                if (offset >= line.Length)
                {
                    if (length >= MinTokenLength) break;
                    line = reader.ReadLine();
                    while (line == "") line = reader.ReadLine();
                    if (line == null)
                    {
                        reader = null;
                        return null;
                    }
                    offset = 0;
                }
                char c = char.ToUpperInvariant(line[offset++]);
                if (char.IsLetterOrDigit(c) || c == '_' || (includeWildcards && (c == '*' || c == '?')))
                {
                    if (length < MaxTokenLength) buffer[length++] = c;
                }
                else if (length >= MinTokenLength) break;
                else length = 0;
            }
            token.SetTermLength(length);
            return token;
        }
    }
}