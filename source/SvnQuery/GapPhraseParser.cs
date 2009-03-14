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

        public Query Parse(string field, TokenStream ts, IndexReader reader)
        {
            IPhrase phrase = Parse(ts);
            if (phrase == null) return null;
            return phrase.BuildQuery(field, reader);
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
            IPhrase leaf = new PhraseTerm(token.TermText());
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
                if (token.TermText().Any(c => c != '*')) // is gap
                {
                    gap = gap < 100 ? gap : 100;
                    return;
                }
                gap += token.TermLength() == 1 ? 1 : 100;
            }
        }

        interface IPhrase
        {
            SpanQuery BuildQuery(string field, IndexReader reader);
        }

        class PhraseTerm : IPhrase
        {
            readonly string text;

            public PhraseTerm(string s)
            {
                text = s;
            }

            public SpanQuery BuildQuery(string field, IndexReader reader)
            {
                Term term = new Term(field, text);

                if (text.All(c => c != '*' && c != '?'))
                    return new SpanTermQuery(term);

                var terms = new List<SpanTermQuery>();
                var termEnum = new WildcardTermEnum(reader, term);
                term = termEnum.Term();
                while (term != null)
                {
                    if (terms.Count > 2000)
                        throw new Exception("too many matches for wildcard query, please be more specific");

                    termEnum.Next();
                    term = termEnum.Term();
                }
                if (terms.Count == 0)
                    return new SpanTermQuery(new Term(FieldName.Path, ":")); // query will never find anything

                return new SpanOrQuery(terms.ToArray());
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

            public SpanQuery BuildQuery(string field, IndexReader reader)
            {
                SpanQuery[] clauses = new SpanQuery[children.Count];
                for (int i = 0; i < clauses.Length; ++i)
                {
                    clauses[i] = children[i].BuildQuery(field, reader);
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