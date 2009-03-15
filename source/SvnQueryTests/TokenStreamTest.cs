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
        public void SimpleTokenStream()
        {
            var ts = new SimpleTokenStream();
            ts.Text = @"Hullebulle * trallala #include <bli\bla\blub>";
            var t = new Token();            

            Assert.That(NextToken(ts, t), Is.EqualTo("HULLEBULLE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("TRALLALA"));
            Assert.That(NextToken(ts, t), Is.EqualTo("INCLUDE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("BLI"));
            Assert.That(NextToken(ts, t), Is.EqualTo("BLA"));
            Assert.That(NextToken(ts, t), Is.EqualTo("BLUB"));
            Assert.That(NextToken(ts, t), Is.Null);
        }
        
        [Test]
        public void SimpleWildcardTokenStream()
        {
            var ts = new SimpleWildcardTokenStream();
            ts.Text = @"Hull*bulle tra??ala * bla";
            var t = new Token();            

            Assert.That(NextToken(ts, t), Is.EqualTo("HULL*BULLE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("TRA??ALA"));
            Assert.That(NextToken(ts, t), Is.EqualTo("*"));
            Assert.That(NextToken(ts, t), Is.EqualTo("BLA"));
            Assert.That(NextToken(ts, t), Is.Null);
        }

        [Test]
        public void PathTokenStream()
        {
            var ts = new PathTokenStream();
            ts.Text = "/Internals/White Space/str#nge.net/file.des*n?r.ext";
            var t = new Token();

            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("INTERNALS"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("WHITE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("SPACE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("STR#NGE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("."));
            Assert.That(NextToken(ts, t), Is.EqualTo("NET"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("FILE"));
            Assert.That(NextToken(ts, t), Is.EqualTo("."));
            Assert.That(NextToken(ts, t), Is.EqualTo("DES*N?R"));
            Assert.That(NextToken(ts, t), Is.EqualTo("."));
            Assert.That(NextToken(ts, t), Is.EqualTo("EXT"));
            Assert.That(NextToken(ts, t), Is.Null);
        }

        [Test]
        public void ExternalsTokenStream()
        {
            PathTokenStream ts = new PathTokenStream();
            ts.Text = "-r5000 ^/Internals/shared/ shared" + Environment.NewLine
                      + "svn://moria/export mcl/dlls";
            Token t = new Token();

            Assert.That(NextToken(ts, t), Is.EqualTo("-R5000"));
            Assert.That(NextToken(ts, t), Is.EqualTo("^"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("INTERNALS"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("SHARED"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("SHARED"));
            Assert.That(NextToken(ts, t), Is.EqualTo("SVN"));
            Assert.That(NextToken(ts, t), Is.EqualTo(":"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/")); 
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("MORIA"));
            Assert.That(NextToken(ts, t), Is.EqualTo("/"));
            Assert.That(NextToken(ts, t), Is.EqualTo("EXPORT"));
            Assert.That(NextToken(ts, t), Is.EqualTo("MCL")); 
            Assert.That(NextToken(ts, t), Is.EqualTo("/")); 
            Assert.That(NextToken(ts, t), Is.EqualTo("DLLS")); 
            Assert.That(NextToken(ts, t), Is.Null);
        }

        [Test]
        public void Empty()
        {
            SimpleTokenStream ts = new SimpleTokenStream();
            ts.Text = "";
            Assert.IsNull(ts.Next());
        }
    }
}