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
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace SvnQuery
{
    public class Parser
    {
        readonly IndexReader reader;
        readonly GapPhraseParser phraseParser;

        public Parser(IndexReader r)
        {
            reader = r;
            phraseParser = new GapPhraseParser();
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

                    if (q != null) query.Add(q, clause);
                    field = outerField;
                    clause = BooleanClause.Occur.MUST;
                }
            }
            return query;
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
                    return ParseSimpleTerm(FieldName.Content, term);
                case "p":
                case "path":
                    return ParsePathTerm(FieldName.Path, term);
                case "e":
                case "x":
                case "externals":
                    return ParsePathTerm(FieldName.Externals, term);
                case "m":
                case "message":
                    return ParseSimpleTerm(FieldName.Message, term);
                case "t":
                case "type":
                case "mime-type":
                    return ParseTypeTerm(term);
                default:
                    return ParseSimpleTerm(field.Replace('_', ':'), term);
            }
        }

        public Query ParseContentOrPathTerm(string term)
        {
            var path = ParsePathTerm(FieldName.Path, term);

            // Heuristic to detect path terms, scan for 
            if (Regex.IsMatch(term, @"(^/|\.)|(/$)|(\*\.)|(\.\*)|(/\*\*/)"))
                return path;

            var content = ParseSimpleTerm(FieldName.Content, term);
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

        public Query ParseSimpleTerm(string field, string term)
        {
            return phraseParser.Parse(field, new SimpleWildcardTokenStream {Text = term}, reader);
        }

        public Query ParsePathTerm(string field, string path)
        {
            return phraseParser.Parse(field, new PathTokenStream {Text = path}, reader);
        }

        public static Query ParseAuthorTerm(string author)
        {
            return new TermQuery(new Term(FieldName.Author, author));
        }

        public static Query ParseTypeTerm(string type)
        {
            return new WildcardQuery(new Term(FieldName.MimeType, type));
        }
    }
}