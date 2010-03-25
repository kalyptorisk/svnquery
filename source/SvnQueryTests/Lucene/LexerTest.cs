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

using Lucene.Net.Search;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using SvnQuery.Lucene;

namespace SvnQuery.Tests.Lucene
{
    [TestFixture]
    public class LexerTest
    {
        static string NextTermToken(Lexer l)
        {
            return ((Lexer.TermToken) l.NextToken()).Text;
        }

        static string NextFieldToken(Lexer l)
        {
            return ((Lexer.FieldToken) l.NextToken()).Text;
        }

        static BooleanClause.Occur NextOperatorToken(Lexer l)
        {
            return ((Lexer.OperatorToken) l.NextToken()).Clause;
        }

        [Test]
        public void SimpleTokens()
        {
            var l = new Lexer(" The quick brown fox  ");

            Assert.AreEqual("The", NextTermToken(l));
            Assert.AreEqual("quick", NextTermToken(l));
            Assert.AreEqual("brown", NextTermToken(l));
            Assert.AreEqual("fox", NextTermToken(l));
            Assert.IsNull(l.NextToken());
        }

        [Test]
        public void Escaping()
        {
            var l = new Lexer("c:\"The:-((quick +brown -#fox()\" bla");

            Assert.AreEqual("c", NextFieldToken(l));
            Assert.AreEqual("The:-((quick +brown -#fox()", NextTermToken(l));
            Assert.AreEqual("bla", NextTermToken(l));
            Assert.IsNull(l.NextToken());
        }

        [Test]
        public void EmbeddedEscaping()
        {
            var l = new Lexer("hulle\"bli bla blub\"bulle");
            Assert.AreEqual("hullebli bla blubbulle", NextTermToken(l));

            l = new Lexer("hulle \"bli bla blub\" bulle"); // note the spaces
            Assert.AreEqual("hulle", NextTermToken(l));
            Assert.AreEqual("bli bla blub", NextTermToken(l));
            Assert.AreEqual("bulle", NextTermToken(l));
        }

        [Test]
        public void PropertyField()
        {
            var l = new Lexer("\"svn:ignore\":bla");
            Assert.That(NextFieldToken(l), Is.EqualTo("svn:ignore"));
            Assert.That(NextTermToken(l), Is.EqualTo("bla"));
        }

        [Test]
        public void EmbeddedOperator()
        {
            var l = new Lexer("-bla #(+hullebulle#p:.cpp)");

            Assert.AreEqual(BooleanClause.Occur.MUST_NOT, NextOperatorToken(l));
            Assert.AreEqual("bla", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.SHOULD, NextOperatorToken(l));
            Assert.IsAssignableFrom(typeof (Lexer.LeftToken), l.NextToken());
            Assert.AreEqual(BooleanClause.Occur.MUST, NextOperatorToken(l));
            Assert.AreEqual("hullebulle", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.SHOULD, NextOperatorToken(l));
            Assert.AreEqual("p", NextFieldToken(l));
            Assert.AreEqual(".cpp", NextTermToken(l));
            Assert.IsAssignableFrom(typeof (Lexer.RightToken), l.NextToken());
            Assert.IsNull(l.NextToken());
        }

        [Test]
        public void Normal()
        {
            var l = new Lexer("/shared/ -bli #bla +blub");

            Assert.AreEqual("/shared/", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.MUST_NOT, NextOperatorToken(l));
            Assert.AreEqual("bli", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.SHOULD, NextOperatorToken(l));
            Assert.AreEqual("bla", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.MUST, NextOperatorToken(l));
            Assert.AreEqual("blub", NextTermToken(l));
        }

        [Test]
        public void NormalEscaped()
        {
            var l = new Lexer("\"/shared/\" -\"bli\" #\"bla\" +\"blub\"");

            Assert.AreEqual("/shared/", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.MUST_NOT, NextOperatorToken(l));
            Assert.AreEqual("bli", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.SHOULD, NextOperatorToken(l));
            Assert.AreEqual("bla", NextTermToken(l));
            Assert.AreEqual(BooleanClause.Occur.MUST, NextOperatorToken(l));
            Assert.AreEqual("blub", NextTermToken(l));
        }
    }
}