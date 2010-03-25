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

namespace SvnQuery.Tests.Lucene
{
    [TestFixture]
    public class RevisionFilterTest
    {
        [Test]
        public void HeadRevisionExist_Flip_cs()
        {
            TestIndex.AssertQueryFromHeadRevision("p:flip.cs", 14);
        }

        [Test]
        public void DeletedFile_DoesNotExistInHeadRevision()
        {
            TestIndex.AssertQueryFromHeadRevision("p:deleted.cpp");
        }

        [Test]
        public void FindFirstRevision()
        {
            TestIndex.AssertQueryFromRevision(1, "p:bla.cpp", 21);
            TestIndex.AssertQueryFromRevision(3, "p:bla.cpp", 21);
            TestIndex.AssertQueryFromRevision(5, "p:bla.cpp", 21);
        }

        [Test]
        public void FindBorderRevision()
        {
            TestIndex.AssertQueryFromRevision(6, "p:bla.cpp", 22);
            TestIndex.AssertQueryFromRevision(7, "p:bla.cpp", 23);
            TestIndex.AssertQueryFromRevision(8, "p:bla.cpp", 23);
        }

        [Test]
        public void DeletedFile_FoundInRevision6to8()
        {
            TestIndex.AssertQueryFromRevision(int.MaxValue, "p:deleted.cpp"); // HEAD should find nothing   
            TestIndex.AssertQueryFromRevision(0, "p:deleted.cpp", 25); // ALL should find it
            TestIndex.AssertQueryFromRevision(5, "p:deleted.cpp");
            TestIndex.AssertQueryFromRevision(6, "p:deleted.cpp", 25);
            TestIndex.AssertQueryFromRevision(7, "p:deleted.cpp", 25);
            TestIndex.AssertQueryFromRevision(8, "p:deleted.cpp", 25);
            TestIndex.AssertQueryFromRevision(9, "p:deleted.cpp");
        }

        [Test]
        public void RangeQuery4to8()
        {
            TestIndex.AssertQueryFromRevisionRange(4, 8, ".cpp", 21, 22, 23, 25);
        }
    }
}