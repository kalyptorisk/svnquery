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
using Lucene.Net.Analysis;

namespace SvnQuery.Lucene
{
    /// <summary>
    /// Breaks text into tokens. A token is either a consecutive sequence of 
    /// one or more IsWordCharacter() or it is a single IsDelimiterCharacter()
    /// </summary>
    /// <remarks>
    /// No need to override Reset() or Close()
    /// </remarks>
    public class SimpleTokenStream : TokenStream
    {
        const int MinTokenLength = 1;
        const int MaxTokenLength = 100;

        string _text;
        int _offset;

        public string Text
        {
            set
            {
                _text = value;
                _offset = 0;
            }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(_text); }
        }

        public override Token Next(Token token)
        {
            token.Clear();

            int length = 0;
            char[] buffer = token.TermBuffer();
            if (buffer.Length < MaxTokenLength) buffer = token.ResizeTermBuffer(MaxTokenLength);

            while (_offset < _text.Length)
            {
                char c = NormalizeCharacter(_text[_offset++]);

                if (length == 0 && (IsFirstCharacter(c) || IsDelimiterCharacter(c)))
                {
                    if (length < MaxTokenLength) buffer[length++] = c;
                    if (IsDelimiterCharacter(c)) break;
                }
                else if (IsWordCharacter(c))
                {
                    if (length < MaxTokenLength) buffer[length++] = c;
                } 
                else if (IsLastCharacter(c))
                {
                    if (length < MaxTokenLength) buffer[length++] = c;
                    break;
                }
                else if (length > 0) // token ready
                {
                    if (IsFirstCharacter(c) || IsDelimiterCharacter(c)) --_offset; // read again
                    break;
                }
            }
            if (length < MinTokenLength) return null;

            token.SetTermLength(length);
            return token;
        }

        protected virtual char NormalizeCharacter(char c)
        {
            return char.ToUpperInvariant(c);
        }

        protected virtual bool IsWordCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        protected virtual bool IsDelimiterCharacter(char c)
        {
            return false;
        }

        protected virtual bool IsFirstCharacter(char c)
        {
            return false;
        }

        protected virtual bool IsLastCharacter(char c)
        {
            return false;
        }

    }

    public class SimpleWildcardTokenStream: SimpleTokenStream
    {
        protected override bool IsWordCharacter(char c)
        {
            return base.IsWordCharacter(c) || (c == '*' || c == '?');
        }
    }

    public class PathTokenStream: SimpleTokenStream
    {
        protected override char NormalizeCharacter(char c)
        {
            return c == '\\' ? '/' : base.NormalizeCharacter(c);
        }

        protected override bool IsWordCharacter(char c)
        {
            return !(char.IsWhiteSpace(c) || (c == '/' || c == '.' || c == ':' || c == '^'));
        }

        protected override bool IsDelimiterCharacter(char c)
        {
            return c == ':' || c == '^';
        }

        protected override bool IsFirstCharacter(char c)
        {
            return c == '.';
        }

        protected override bool IsLastCharacter(char c)
        {
            return c == '/';
        }


    }
}