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
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;

namespace SvnQuery.Lucene
{
    public class GapPhraseParser
    {
        TokenStream _stream;
        Token _token;
        int _gap;

        public SpanQuery Parse(string field, TokenStream ts, IndexReader reader)
        {
            IPhrase phrase = Parse(ts);
            if (phrase == null) return null;
            return phrase.BuildQuery(field, reader, true, true);
        }

        public string ParseToString(TokenStream ts)
        {
            IPhrase phrase = Parse(ts);
            return phrase == null ? "" : phrase.ToString();
        }

        IPhrase Parse(TokenStream ts)
        {
            _stream = ts;
            _token = new Token();
            NextGap(); // Move _token to the next gap, ignore leading gaps
            if (_token == null) return null; // expression always false is an resharper bug
            return Parse(int.MaxValue);
        }

        /// <summary>
        /// Give me the highest gap smaller than maxGap
        /// </summary>
        IPhrase Parse(int maxGap)
        {
            IPhrase leaf = new TermPhrase(_token.TermText());
            NextGap();
            while (_gap < maxGap)
            {
                GapPhrase node = new GapPhrase(_gap);
                node.AddChild(leaf);
                node.AddChild(Parse(node.Gap));
                while (_gap == node.Gap)
                {
                    node.AddChild(Parse(node.Gap));
                }
                leaf = node;
            }
            return leaf;
        }

        void NextGap()
        {
            _gap = 0;
            while (true)
            {
                _token = _stream.Next(_token);
                if (_token == null)
                {
                    _gap = int.MaxValue;
                    return;
                }

                int gapLength = GapLength();

                if (gapLength == 0)
                {
                    _gap = _gap < 100 ? _gap : 100;
                    return;
                }
                _gap += gapLength;
            }
        }

        int GapLength()
        {
            int len = _token.TermLength();
            char[] buffer = _token.TermBuffer();

            int i = 0;
            while (buffer[i] == '*' && i < len) ++i;
            if (i > 0 && (i == len || (i == --len && buffer[len] == '/'))) 
                return i == 1 ? 1 : 100;

            return 0;
        }

        interface IPhrase
        {
            /// <summary>
            /// Builds a query for the contained phrase
            /// </summary>
            /// <param name="field">the lucene field for which the query should be build</param>
            /// <param name="reader">an IndexReader for enumerating wildcard terms</param>
            /// <param name="isFirst">true if this is the first phrase in a span</param>
            /// <param name="isLast">true if this is the last phrase in a span</param>
            /// <returns></returns>
            SpanQuery BuildQuery(string field, IndexReader reader, bool isFirst, bool isLast);
        }

        class TermPhrase : IPhrase
        {
            readonly string _text;

            public TermPhrase(string s)
            {
                _text = s;
            }

            public SpanQuery BuildQuery(string field, IndexReader reader, bool isFirst, bool isLast)
            {
                var terms = new List<SpanTermQuery>(WildcardTerms(PathTerms(field, isFirst, isLast), reader));

                if (terms.Count == 0) // return a query that will never find anything (':' is an invalid path character)
                    return new SpanTermQuery(new Term(FieldName.Path, ":"));
                if (terms.Count == 1)
                    return terms[0];

                return new SpanOrQuery(terms.ToArray());
            }

            IEnumerable<Term> PathTerms(string field, bool isFirst, bool isLast)
            {
                if (field == FieldName.Path || field == FieldName.Externals)
                {
                    foreach (string s in PathVariants(isFirst, isLast))
                    {
                        yield return new Term(field, s);
                    }
                }
                else
                {
                    yield return new Term(field, _text);
                }
            }

            IEnumerable<string> PathVariants(bool isFirst, bool isLast)
            {
                yield return _text;
                
                if (isFirst && _text[0] != '.' && _text[0] != '/')
                    yield return "." + _text;

                if (isLast && _text[_text.Length - 1] != '/')
                {
                    yield return _text + "/";

                    if (isFirst && _text[0] != '.') 
                        yield return "." + _text + "/";
                }
            }

            bool HasWildcards
            {
                get { return _text.Any(c => c == '*' || c == '?'); }
            }
        
            IEnumerable<SpanTermQuery> WildcardTerms(IEnumerable<Term> terms, IndexReader reader)
            {
                if (!HasWildcards)
                {
                    foreach (Term t in terms)
                    {
                        yield return new SpanTermQuery(t);
                    }
                }
                else
                {
                    int maxTerms = 1000;
                    foreach (Term t in terms)
                    {
                        var termEnum = new WildcardTermEnum(reader, t);
                        var term = termEnum.Term();
                        while (term != null)
                        {
                            if (--maxTerms < 0)
                                throw new Exception("too many matches for wildcard query, please be more specific");

                            yield return new SpanTermQuery(term);
                            termEnum.Next();
                            term = termEnum.Term();
                        }
                    }
                }
            }

            public override string ToString()
            {
                return _text;
            }

        }

        class GapPhrase : IPhrase
        {
            readonly List<IPhrase> _children = new List<IPhrase>();

            public GapPhrase(int gap)
            {
                Gap = gap;
            }

            public readonly int Gap;

            public void AddChild(IPhrase child)
            {
                _children.Add(child);
            }

            public SpanQuery BuildQuery(string field, IndexReader reader, bool isFirst, bool isLast)
            {
                SpanQuery[] clauses = new SpanQuery[_children.Count];
                int lastClause = clauses.Length - 1;
                for (int i = 0; i < clauses.Length; ++i)
                {
                    clauses[i] = _children[i].BuildQuery(field, reader, i == 0, i == lastClause);
                }
                if (Gap > 0) // try to remove overlappings through SpanNotQueries
                {
                    for (int i = 0; i < clauses.Length; ++i)
                    {
                        if (i < lastClause && clauses[i].GetTerms().Count <= clauses[i + 1].GetTerms().Count)
                        {
                            clauses[i] = new SpanNotQuery(clauses[i], clauses[i + 1]);
                        }
                        if (i > 0 && clauses[i - 1].GetTerms().Count > clauses[i].GetTerms().Count)
                        {
                            clauses[i] = new SpanNotQuery(clauses[i], clauses[i - 1]);
                        }
                    }
                }

                return new SpanNearQuery(clauses, Gap, true);
            }

            public override string ToString() // needed for debugging and unit tests
            {
                if (_children.Count == 1) return _children[0].ToString();

                StringBuilder sb = new StringBuilder();
                sb.Append('(');
                for (int i = 0; i < _children.Count; ++i)
                {
                    sb.Append(_children[i]);
                    if (i < _children.Count - 1)
                    {
                        if (Gap < 0) sb.Append('$');
                        else if (Gap < 1) sb.Append(' ');
                        else if (Gap < 100)
                        {
                            for (int j = 0; j < Gap; ++j) sb.Append(" *");
                            sb.Append(' ');
                        }
                        else sb.Append(" ** ");
                    }
                }
                sb.Append(')');
                return sb.ToString();
            }
        }

    }
}