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

using System.Diagnostics;
using System.Text;
using Lucene.Net.Search;

namespace SvnQuery
{
    public class Lexer
    {
        readonly string input;
        readonly StringBuilder text;
        int offset;

        public Lexer(string s)
        {
            input = s;
            offset = 0;
            text = new StringBuilder();
        }

        public class Token { }
        public class LeftToken : Token { }
        public class RightToken : Token { }
        public class OperatorToken : Token
        {
            public BooleanClause.Occur Clause;
        }
        public class TermToken : Token
        {
            public string Text;
        }
        public class FieldToken: Token
        {
            public string Text;
        }

        public Token NextToken()
        {
            text.Length = 0;
            bool escaping = false;
            while (offset < input.Length)
            {
                char c = input[offset++];

                if (text.Length == 0 && !escaping)
                {
                    if (char.IsWhiteSpace(c)) continue;
                    if (c == '+') return new OperatorToken { Clause = BooleanClause.Occur.MUST };
                    if (c == '#') return new OperatorToken { Clause = BooleanClause.Occur.SHOULD};
                    if (c == '-') return new OperatorToken { Clause = BooleanClause.Occur.MUST_NOT};
                    if (c == '(') return new LeftToken();
                    if (c == ')') return new RightToken();
                    if (c == '"')
                        escaping = true;
                    else
                        text.Append(c);
                }
                else
                {
                    if (escaping)
                    {
                        if (c == '"')
                        {
                            escaping = false;
                            continue;                            
                        }                            
                    }
                    else
                    {
                        if (c == '"')
                        {
                            escaping = true;
                            continue;
                        }
                        Debug.Assert(text.Length > 0);
                        if (c == ':')
                        {
                            return new FieldToken { Text = text.ToString() };                            
                        }
                        if (char.IsWhiteSpace(c) || "()+#-".IndexOf(c) >= 0)
                        {
                            --offset;
                            break;
                        }
                    }
                    text.Append(c);
                }
            }
            return text.Length == 0 ? null : new TermToken { Text = text.ToString() };
        }

    }
}
