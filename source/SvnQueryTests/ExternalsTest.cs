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
using Lucene.Net.Analysis;
using NUnit.Framework;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class ExternalsTest
    {
        static string NextToken(TokenStream s, Token t)
        {
            t = s.Next(t);
            return t.TermText();
        }

        [Test]
        public void TokenStream()
        {
            const string eol = ExternalsTokenStream.Eol;
            ExternalsTokenStream ts = new ExternalsTokenStream();
            ts.SetText("/Internals/shared/ shared" + Environment.NewLine + "/Internals/MCL/export mcl/dlls");

            Token t = new Token();

            Assert.AreEqual(eol, NextToken(ts, t));

            Assert.AreEqual("internals", NextToken(ts, t));
            Assert.AreEqual("shared", NextToken(ts, t));

            Assert.AreEqual(eol, NextToken(ts, t));

            Assert.AreEqual("internals", NextToken(ts, t));
            Assert.AreEqual("mcl", NextToken(ts, t));
            Assert.AreEqual("export", NextToken(ts, t));

            Assert.AreEqual(eol, NextToken(ts, t));
        }

        [Test]
        public void Empty()
        {
            ExternalsTokenStream ts = new ExternalsTokenStream();
            ts.SetText("");
            Assert.IsNull(ts.Next());
        }

        [Test]
        public void DirectReference()
        {
            TestIndex.AssertQuery("x:/shared/general", 18, 19);
        }

        [Test]
        public void SubReference()
        {
            TestIndex.AssertQuery("x:/shared", 18, 19, 20);
        }

        [Test]
        public void NonFirstReference()
        {
            TestIndex.AssertQuery("x:/woanders", 19);
        }
    }
}