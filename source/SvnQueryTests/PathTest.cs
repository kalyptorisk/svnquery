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

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using NUnit.Framework;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class PathTest
    {
        [Test]
        public void ManualSpanQuery()
        {
            SpanQuery shared = new SpanTermQuery(new Term(FieldName.Path, "shared/"));
            SpanQuery fileio = new SpanTermQuery(new Term(FieldName.Path, "fileio"));
            SpanQuery firstSpan = new SpanNearQuery(new[] {shared, fileio}, 10, true);
            SpanQuery cpp = new SpanTermQuery(new Term(FieldName.Path, ".cpp"));
            SpanQuery h = new SpanTermQuery(new Term(FieldName.Path, ".h"));
            SpanQuery xml = new SpanTermQuery(new Term(FieldName.Path, ".xml"));
            SpanQuery ext = new SpanOrQuery(new[] {cpp, h, xml}); // simulates .*
            Query q = new SpanNearQuery(new[] {firstSpan, ext}, 0, true);

            TestIndex.AssertQuery(q, 1, 3, 4, 5, 8, 9);
        }

        static Query PathQuery(string query)
        {
            Parser p = new Parser(TestIndex.Reader);
            return p.ParsePathTerm(query) ?? new TermQuery(new Term("never", "never"));
        }

        [Test]
        public void StartAtRoot()
        {
            TestIndex.AssertQuery(PathQuery("/shared/"), 1, 2, 3, 4, 5, 6, 7);
        }

        [Test]
        public void CsExtension()
        {
            TestIndex.AssertQuery(PathQuery(".cs"), 0, 14, 17);
        }

        [Test]
        public void LeadingWildcards()
        {
            TestIndex.AssertQuery(PathQuery("*.cs"), 0, 14, 17);
            TestIndex.AssertQuery(PathQuery("**/*.cs"), 0, 14, 17);
        }

        [Test]
        public void PathGap()
        {
            TestIndex.AssertQuery(PathQuery("FileIO/**/fileio.cpp"), 1, 8, 15);
            TestIndex.AssertQuery(PathQuery("/woanders/FileIO/**/fileio.*"), 15, 16);
        }

        [Test]
        public void SingleGap()
        {
            TestIndex.AssertQuery(PathQuery("/shared/general/FileIO/*.xml"), 4);
        }

        [Test]
        public void MultiGap()
        {
            TestIndex.AssertQuery(PathQuery("**/shared/general/*/*.xml"), 4);
            TestIndex.AssertQuery(PathQuery("**/shared/**/general/*/*.xml"), 4);
        }


        [Test]
        public void PartialPath()
        {
            TestIndex.AssertQuery(PathQuery("selt.sam/source/form1.design.cs"), 17);
        }

        [Test]
        public void EmptyQuery()
        {
            TestIndex.AssertQuery(PathQuery(""));
        }
    }
}