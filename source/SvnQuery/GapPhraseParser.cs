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

namespace SvnQuery
{
    public class GapPhraseParser
    {
        TokenStream stream;
        Token token;
        int gap;

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
            stream = ts;
            token = new Token();
            NextGap(); // Ignore leading gaps
            if (token == null) return null;
            return Parse(int.MaxValue);
        }

        /// <summary>
        /// Give me the highest gap smaller than maxGap
        /// </summary>
        IPhrase Parse(int maxGap)
        {
            IPhrase leaf = new TermPhrase(token.TermText());
            NextGap();
            while (gap < maxGap)
            {
                GapPhrase node = new GapPhrase(gap);
                node.AddChild(leaf);
                node.AddChild(Parse(node.Gap));
                while (gap == node.Gap)
                {
                    node.AddChild(Parse(node.Gap));
                }
                leaf = node;
            }
            return leaf;
        }

        void NextGap()
        {
            gap = 0;
            while (true)
            {
                token = stream.Next(token);
                if (token == null)
                {
                    gap = int.MaxValue;
                    return;
                }

                int gapLength = GapLength();

                if (gapLength == 0)
                {
                    gap = gap < 100 ? gap : 100;
                    return;
                }
                gap += gapLength;
            }
        }

        int GapLength()
        {
            int len = token.TermLength();
            char[] buffer = token.TermBuffer();

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
            readonly string text;

            public TermPhrase(string s)
            {
                text = s;
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
                    yield return new Term(field, text);
                }
            }

            public IEnumerable<string> PathVariants(bool isFirst, bool isLast)
            {
                yield return text;
                
                if (isFirst && text[0] != '.' && text[0] != '/')
                    yield return "." + text;

                if (isLast && text[text.Length - 1] != '/')
                {
                    yield return text + "/";

                    if (isFirst && text[0] != '.') 
                        yield return "." + text + "/";
                }
            }

            bool HasWildcards
            {
                get { return text.Any(c => c == '*' || c == '?'); }
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
                return text;
            }

        }

        class GapPhrase : IPhrase
        {
            readonly List<IPhrase> children = new List<IPhrase>();

            public GapPhrase(int gap)
            {
                Gap = gap;
            }

            public readonly int Gap;

            public void AddChild(IPhrase child)
            {
                children.Add(child);
            }

            public SpanQuery BuildQuery(string field, IndexReader reader, bool isFirst, bool isLast)
            {
                SpanQuery[] clauses = new SpanQuery[children.Count];
                int lastClause = clauses.Length - 1;
                for (int i = 0; i < clauses.Length; ++i)
                {
                    clauses[i] = children[i].BuildQuery(field, reader, i == 0, i == lastClause);
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
                if (children.Count == 1) return children[0].ToString();

                StringBuilder sb = new StringBuilder();
                sb.Append('(');
                for (int i = 0; i < children.Count; ++i)
                {
                    sb.Append(children[i]);
                    if (i < children.Count - 1)
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