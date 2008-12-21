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

using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;

namespace SvnQuery
{
    static class Test
    {

        public static void WildcardStress(string index)
        {
            var searcher = new IndexSearcher(index);
            Console.WriteLine("Documents: " + searcher.MaxDoc());


            var terms = new List<SpanTermQuery>();
            var termEnum = new WildcardTermEnum(searcher.Reader, new Term("content", "a*"));
            Term term = termEnum.Term();
            while (term != null)
            {
                Console.WriteLine("Term: " + term.Text());
                terms.Add(new SpanTermQuery(term));
                termEnum.Next();
                term = termEnum.Term();
            }
            Console.WriteLine("TermCount: " + terms.Count);

            SpanQuery q = new SpanOrQuery(terms.ToArray());
            var hits = searcher.Search(q);
            PrintHits(hits);


            //BooleanQuery.SetMaxClauseCount(2000);
            //var q = new WildcardQuery(new Term("content", "a*"));
            //var hits = searcher.Search(q);
            //PrintHits(hits);
        }

        public static void MustNot(string index)
        { 
            var searcher = new IndexSearcher(index);
            Console.WriteLine("Documents: " + searcher.MaxDoc());

            var q = new BooleanQuery();
            q.Add(new TermQuery(new Term("path", "tdm/")), BooleanClause.Occur.SHOULD);
            q.Add(new TermQuery(new Term("path", "uio/")), BooleanClause.Occur.SHOULD);
            q.Add(new TermQuery(new Term("path", ".cpp")), BooleanClause.Occur.MUST_NOT);
            var hits = searcher.Search(q);
            PrintHits(hits);
        }

        public static void FindBinary(string index)
        {
            var searcher = new IndexSearcher(index);
            Console.WriteLine("Documents: " + searcher.MaxDoc());

            var q = new TermQuery(new Term("content", "%BINARY%"));

            var hits = searcher.Search(q);
            PrintHits(hits);

        }

        public static void FindContent(string index)
        {
            var searcher = new IndexSearcher(index);
            Console.WriteLine("Documents: " + searcher.MaxDoc());

            var q = new WildcardQuery(new Term("content", "*sprenger"));

            var hits = searcher.Search(q);
            PrintHits(hits);

        }

        private static void PrintHits(Hits hits)
        {
            if (hits == null)
            {
                Console.WriteLine("no hits");
            }
            else
            {
                Console.WriteLine("Hits: " + hits.Length());
                Console.WriteLine();

                //// iterate over the first 10 results.
                for (int i = 0; i < 100 && i < hits.Length(); i++)
                {
                    Document doc = hits.Doc(i);
                    //string contentValue = doc.Get("content");

                    //Console.WriteLine(contentValue);
                    Console.WriteLine(doc.Get("id"));
                }

            }
            Console.ReadKey();
        }

    }
}
