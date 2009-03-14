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
using NUnit.Framework.SyntaxHelpers;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class GapPhraseParserTest
    {
        static string Parse(string content)
        {
            return new GapPhraseParser().ParseToString(new ContentTokenStream(content, true));
        }

        [Test]
        public void SimplePhrase_A_B_C()
        {           
            Assert.That(Parse("a b c"), Is.EqualTo("(A B C)"));
        }

        [Test]
        public void MultipleSingleGaps_A_star_B_B_star_C_D()
        {
            Assert.That(Parse("a * b b * c d"), Is.EqualTo("(A * (B B) * (C D))"));                       
        }

        [Test]
        public void RepeatedChain()
        {
            Assert.That(Parse("a b * c ** a b * c"), Is.EqualTo("(((A B) * C) ** ((A B) * C))"));
        }

        [Test]
        public void ReverseRepeatedChain()
        {
            Assert.That(Parse("a ** b * c a ** b * c a"), Is.EqualTo("(A ** (B * (C A)) ** (B * (C A)))"));
        }

        [Test]
        public void ExplicitGaps()
        {
            Assert.That(Parse("a * * b * * * c * * d"), Is.EqualTo("((A * * B) * * * (C * * D))"));                        
        }

        [Test]
        public void LeadingGapsAreIgnored()
        {
            Assert.That(Parse("* ** a b c"), Is.EqualTo("(A B C)"));                                                
        }

        [Test]
        public void TrailingGapsAreIgnored()
        {
            Assert.That(Parse("a b c * ** "), Is.EqualTo("(A B C)"));                                                
        }

        [Test]
        public void EmptyPhrase()
        {
            Assert.That(Parse(""), Is.EqualTo(""));                                                
            
        }
    }
}