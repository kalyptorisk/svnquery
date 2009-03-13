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

using Lucene.Net.Analysis;

namespace SvnQuery
{
    /// <summary>
    /// Splits a svn path into its token parts
    /// </summary>
    /// <remarks>
    /// No need to override Reset() or Close()
    /// </remarks>
    public class PathTokenStream : TokenStream
    {
        const int MaxPathComponentLen = 250;
        string path;
        int position;

        public PathTokenStream()
        {}

        public PathTokenStream(string path_to_tokenize)
        {
            Reset(path_to_tokenize);
        }

        public void SetText(string text)
        {
            Reset(text);
        }

        public void Reset(string path_to_tokenize)
        {
            path = path_to_tokenize;
            position = 0;
        }

        public override Token Next(Token token)
        {
            token.Clear();
            if (position >= path.Length) return null;

            token.SetStartOffset(position);
            char[] buffer = token.TermBuffer();
            if (buffer.Length < MaxPathComponentLen) buffer = token.ResizeTermBuffer(MaxPathComponentLen);

            int length = 0;
            while (true)
            {
                char c = char.ToUpperInvariant(path[position++]);
                if (c == '\\') c = '/';
                buffer[length++] = c;
                if (c == '/') break;
                if (position < path.Length) continue;

                // Backtrack
                for (int i = length; --i > 0;)
                {
                    if (buffer[i] == '.')
                    {
                        position -= length - i;
                        length = i;
                        break;
                    }
                }
                break;
            }
            token.SetTermLength(length);
            token.SetEndOffset(position - 1);
            return token;
        }
    }
}