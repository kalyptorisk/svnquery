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
using System.Diagnostics;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;

namespace SvnQuery
{
    public class Parser
    {
        static readonly char[] Wildcards = new[] {'*', '?'};

        readonly IndexReader reader;

        public Parser(IndexReader r)
        {
            reader = r;
        }

        public Query ParsePathTerm(string path)
        {
            var pts = new PathTokenStream(path);

            var span = new List<SpanQuery>();
            SpanQuery q = null;
            int gap = 0;

            for (Token token = pts.Next(); token != null; token = pts.Next(token))
            {
                string text = token.TermText();

                if (Regex.IsMatch(text, @"^\.?\*+/?$")) // single gap, * or .* or */ or pure wildcard pattern
                {
                    if (span.Count > 0)
                    {
                        q = CombineSpans(q, span, gap);
                        span.Clear();
                        gap = 0;
                    }
                    gap += text == "**/" ? 100 : 1;
                }
                else
                {
                    span.Add(WildcardPathTerm(text));
                }
            }
            return span.Count == 0 ? q : CombineSpans(q, span, gap);
        }

        SpanQuery CombineSpans(SpanQuery q, List<SpanQuery> span, int gap)
        {
            SpanQuery current = (span.Count == 1) ? span[0] : new SpanNearQuery(span.ToArray(), 0, true);
            return (q == null) ? current : new SpanNearQuery(new[] {q, current}, gap, true);
        }

        SpanQuery WildcardPathTerm(string text)
        {
            Term term = new Term(FieldName.Path, text);

            if (text.IndexOfAny(Wildcards) < 0)
                return new SpanTermQuery(term);

            bool fileOnly = !text.EndsWith("/");
            var terms = new List<SpanTermQuery>();
            var termEnum = new WildcardTermEnum(reader, term);
            term = termEnum.Term();
            while (term != null)
            {
                if (terms.Count > 2000)
                    throw new Exception("too many matches for wildcard query, please be more specific");

                if (!(fileOnly && term.Text().EndsWith("/")))
                    terms.Add(new SpanTermQuery(term));
                termEnum.Next();
                term = termEnum.Term();
            }
            if (terms.Count == 0)
                return new SpanTermQuery(new Term(FieldName.Path, ":")); // query will never find anything
            return new SpanOrQuery(terms.ToArray());
        }

        Term[] WildcardContentTerms(Term wildcard)
        {
            string text = wildcard.Text();
            if (text.IndexOfAny(Wildcards) < 0) return new[] {wildcard};

            if (Regex.IsMatch(text, @"^[\*\?]*$"))
                throw new Exception("too many matches for wildcard query, please be more specific");

            var terms = new List<Term>();
            var termEnum = new WildcardTermEnum(reader, wildcard);
            Term term = termEnum.Term();
            while (term != null)
            {
                terms.Add(term);
                termEnum.Next();
                term = termEnum.Term();

                if (terms.Count > 2000)
                    throw new Exception("too many matches for wildcard query, please be more specific");
            }
            return terms.ToArray();
        }

        public Query ParseContentTerm(string field, string content)
        {
            var q = new MultiPhraseQuery();
            bool hasTerm = false;

            TokenStream ts = new ContentTokenStream(content, true);
            for (Token token = ts.Next(); token != null; token = ts.Next(token))
            {
                Term[] terms = WildcardContentTerms(new Term(field, token.TermText()));
                if (terms.Length > 0)
                {
                    q.Add(terms);
                    hasTerm = true;
                }
            }
            return (hasTerm) ? q : null;
        }

        public Query ParseContentOrPathTerm(string term)
        {
            var path = ParsePathTerm(term);

            // Heuristic to detect path terms, scan for 
            if (Regex.IsMatch(term, @"(^/|\.)|(/$)|(\*\.)|(\.\*)|(/\*\*/)"))
                return path;

            var content = ParseContentTerm("content", term);
            if (path != null && content != null)
            {
                var q = new BooleanQuery();
                q.Add(path, BooleanClause.Occur.SHOULD);
                q.Add(content, BooleanClause.Occur.SHOULD);
                q.SetMinimumNumberShouldMatch(1);
                return q;
            }
            return path ?? content;
        }

        static Query ParseExternalsTerm(string externals)
        {
            TokenStream ts = new ExternalsTokenStream(externals);
            List<string> parts = new List<string>();
            for (Token token = ts.Next(); token != null; token = ts.Next(token))
            {
                string text = token.TermText();
                if (text != ExternalsTokenStream.Eol) parts.Add(text);
            }

            if (parts.Count == 0) return null;

            if (parts.Count == 1) return new TermQuery(new Term(FieldName.Externals, parts[0]));

            var bq = new BooleanQuery();
            Term eop = new Term(FieldName.Externals, ExternalsTokenStream.Eol);
            for (int i = 0; i++ < parts.Count;)
            {
                var q = new PhraseQuery();
                q.Add(eop);
                for (int j = 0; j < i; j++)
                {
                    q.Add(new Term(FieldName.Externals, parts[j]));
                }
                if (i < parts.Count) q.Add(eop);
                bq.Add(q, BooleanClause.Occur.SHOULD);
            }
            return bq;
        }

        public static Query ParseAuthorTerm(string author)
        {
            return new TermQuery(new Term(FieldName.Author, author));
        }

        public static Query ParseTypeTerm(string type)
        {
            return new WildcardQuery(new Term(FieldName.Type, type));
        }

        public Query ParseTerm(string term, string field)
        {
            if (field == null) return ParseContentOrPathTerm(term);

            switch (field)
            {
                case "a":
                case "author":
                    return ParseAuthorTerm(term);
                case "c":
                case "content":
                    return ParseContentTerm(FieldName.Content, term);
                case "p":
                case "path":
                    return ParsePathTerm(term);
                case "e":
                case "x":
                case "externals":
                    return ParseExternalsTerm(term);
                case "m":
                case "message":
                    return ParseContentTerm(FieldName.Message, term);
                case "t":
                case "type":
                case "mime-type":
                    return ParseTypeTerm(term);
                default:
                    return ParseContentTerm(field.Replace('_', ':'), term);
            }
        }

        /// <summary>
        /// translate a human query into a lucene query
        /// </summary>
        /// <remarks>
        /// see help page for syntax
        /// </remarks>
        public Query Parse(string query)
        {
            return Parse(new Lexer(query), null);
        }

        BooleanQuery Parse(Lexer lexer, string outerField)
        {
            BooleanClause.Occur clause = BooleanClause.Occur.MUST;
            string field = outerField;

            BooleanQuery query = new BooleanQuery();

            for (var t = lexer.NextToken(); t != null; t = lexer.NextToken())
            {
                if (t is Lexer.OperatorToken)
                {
                    clause = ((Lexer.OperatorToken) t).Clause;
                }
                else if (t is Lexer.FieldToken)
                {
                    field = ((Lexer.FieldToken) t).Text;
                }
                else if (t is Lexer.RightToken)
                {
                    break;
                }
                else // Term or SubExpression
                {
                    Query q;
                    if (t is Lexer.TermToken)
                    {
                        q = ParseTerm(((Lexer.TermToken) t).Text, field);
                    }
                    else if (t is Lexer.LeftToken)
                    {
                        q = Parse(lexer, field);
                    }
                    else throw new InvalidOperationException("Unexpected token");

                    Debug.Assert(q != null);
                    query.Add(q, clause);
                    field = outerField;
                    clause = BooleanClause.Occur.MUST;
                }
            }
            return query;
        }
    }
}