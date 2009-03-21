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

using NUnit.Framework;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class ContentTest
    {
        [Test]
        public void WildcardStar()
        {
            TestIndex.AssertQueryFromHeadRevision("c:El*ant", 12, 15, 16, 17);
        }
        
        [Test]
        public void WildcardsQuestionMark()
        {
            TestIndex.AssertQueryFromHeadRevision("c:Ele?ant", 15, 16, 17);
        }

        [Test]
        public void Gap()
        {
            TestIndex.AssertQueryFromHeadRevision("c:\"comment ** that", 8, 9);            
        }

        [Test]
        public void RealLifeIncludeQuery()
        {
            TestIndex.AssertQueryFromHeadRevision(@"c:""#include ** bla.h""", 9);            
        }

        [Test]
        public void RealLifeFulltext()
        {
            TestIndex.AssertQueryFromHeadRevision(@"c:""include ** comment ** searched""", 8);
        } 
        
        [Test]
        public void RealLifeGrouping()
        {
            TestIndex.AssertQueryFromHeadRevision(@"c:(include comment searched)", 8);
            TestIndex.AssertQueryFromHeadRevision(@"c:(include comment)", 8, 9);
            TestIndex.AssertQueryFromHeadRevision(@"c:(include comment -searched)", 9);
        }
    }
}