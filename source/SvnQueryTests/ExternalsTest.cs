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
    public class ExternalsTest
    {      

        [Test]
        public void DirectReference__shared_general()
        {
            TestIndex.AssertQueryFromHeadRevision("x:/shared/general", 19);
        }

        [Test]
        public void SubReference__shared()
        {
            TestIndex.AssertQueryFromHeadRevision("x:/shared", 18, 19, 20);
        }

        [Test]
        public void NonFirstReference_woanders()
        {
            TestIndex.AssertQueryFromHeadRevision("x:/woanders", 19);
        }

        [Test]
        public void LocalFolder_Found()
        {
            TestIndex.AssertQueryFromHeadRevision("x:\"localfolder\"", 17);            
        }

        [Test]
        public void AbsoluteExternal_Found()
        {
            TestIndex.AssertQueryFromHeadRevision("x:\"svn://svnquery.tigris.org\"", 17);            
        }

        [Test]
        public void FixedExternals()
        {
            TestIndex.AssertQueryFromHeadRevision("x:\"-r5000\"", 16);
        }

        [Test]
        public void FixedExternalsWildcards()
        {
            TestIndex.AssertQueryFromHeadRevision("x:\"-r*\"", 16, 15);
        }

        [Test]
        public void SomePathInFixedExternals()
        {
            TestIndex.AssertQueryFromHeadRevision("x:products/internal", 16);
        }
        

    }
}