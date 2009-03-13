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
using NUnit.Framework;
using Lucene.Net.Analysis;
using NUnit.Framework.SyntaxHelpers;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class TokenStreamTest
    {
        static string NextToken(TokenStream s, Token t)
        {
            t = s.Next(t);
            return t == null ? null : t.TermText();
        }

        [Test]
        public void PathTokenStream()
        {
            //var ts = new PathTokenStreamEx();
            //ts.SetText("/Internals/White Space/str#nge.net/file.des*n?r.ext");
            //var t = new Token();

            //Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("INTERNALS"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("WHITE"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("SPACE"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("STR#NGE"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("."));
            //Assert.That(NextToken(ts, t), Is.EqualTo("NET"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("FILE"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("."));
            //Assert.That(NextToken(ts, t), Is.EqualTo("DES*N?R"));
            //Assert.That(NextToken(ts, t), Is.EqualTo("."));
            //Assert.That(NextToken(ts, t), Is.EqualTo("EXT"));
            //Assert.That(NextToken(ts, t), Is.Null);
        }

        [Test]
        public void ExternalsTokenStream()
        {
            //const string eol = PathTokenStream.Eol;
            //SimpleTokenStream ts = new SimpleTokenStream();
            //ts.SetText("^/Internals/shared/ shared" + Environment.NewLine 
            //         + "svn://moria/Internals/MCL/export mcl/dlls");

            //Token t = new Token();

            //Assert.AreEqual(eol, NextToken(ts, t));

            //Assert.AreEqual("internals", NextToken(ts, t));
            //Assert.AreEqual("shared", NextToken(ts, t));

            //Assert.AreEqual(eol, NextToken(ts, t));

            //Assert.AreEqual("internals", NextToken(ts, t));
            //Assert.AreEqual("mcl", NextToken(ts, t));
            //Assert.AreEqual("export", NextToken(ts, t));

            //Assert.AreEqual(eol, NextToken(ts, t));
        }

        [Test]
        public void Empty()
        {
            //SimpleTokenStream ts = new SimpleTokenStream();
            //ts.SetText("");
            //Assert.IsNull(ts.Next());
        }
    }
}