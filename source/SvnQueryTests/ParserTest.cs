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
using NUnit.Framework;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class ParserTest
    {
        readonly Parser parser;

        public ParserTest()
        {
            parser = new Parser(TestIndex.Reader);
        }

        void AssertParserQuery(string query, params int[] result)
        {
            Console.WriteLine("Query: " + query);
            TestIndex.AssertQuery(parser.Parse(query), result);
        }

        [Test]
        public void SimpleMust()
        {
            AssertParserQuery("trommelklo Elefant ", 17);
        }

        [Test]
        public void SimpleShould()
        {
            AssertParserQuery("Katze Cat");
            AssertParserQuery("#Katze #Cat", 13, 14, 15, 16);
        }

        [Test]
        public void SimpleMustNot()
        {
            AssertParserQuery(".cpp -Elefant", 1, 2, 7, 8, 11, 12, 24, 26);
        }

        [Test]
        public void ResetLocalOperator()
        {
            AssertParserQuery("  #Elefant Trommelklo", 17);
            AssertParserQuery(" (#Elefant Trommelklo)", 17);
            AssertParserQuery("+(#Elefant Trommelklo)", 17);
        }

        [Test]
        public void NestedQuery()
        {
            AssertParserQuery("cat +(#.h #.cpp))", 13, 15);
        }

        [Test]
        public void MisplacedBrackets()
        {
            AssertParserQuery("cat +(#.h #.cpp))))", 13, 15); // ignore too many or to few brackets
            AssertParserQuery("cat +(#.h #.cpp", 13, 15); // ignore too many or to few brackets
        }

        [Test]
        public void TooManyOperators()
        {
            AssertParserQuery("-#+Elefant -#+Cat", 15);
        }

        [Test]
        public void SimpleFieldQuery()
        {
            AssertParserQuery("p:Flip.cs", 14);
            AssertParserQuery("p:Flip", 14);
        }

        [Test]
        public void QualifiedContent()
        {
            AssertParserQuery("c:(#max #moritz #anders)", 10, 11);
        }

        [Test]
        public void Phrase()
        {
            AssertParserQuery("\"max und moritz\"", 10);
            AssertParserQuery("c:\"moritz und max\"", 11);
        }
    }
}