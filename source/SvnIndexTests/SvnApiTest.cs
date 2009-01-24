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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using SvnQuery;

namespace SvnIndexTests
{
    [TestFixture]
    public class SvnApiTest
    {
        static readonly string repository;
        readonly ISvnApi api = new SharpSvnApi(repository);

        static SvnApiTest()
        {
            // works only if shadow copying in unit tests is disabled, because otherwise the relative path is wrong
            repository = "file:///" + Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\test_repository"));
        }

        /// <summary>
        /// Tests a bug in the subversion or sharpsvn code that seems to be present only on file based (windows) repositories
        /// </summary>
        [Test, Ignore]
        public void GetPathData_InvalidPathInSubversion1398_NoException()
        {
            ISvnApi svn = new SharpSvnApi("file:///d:/SvnMirror");
            const string path = "/trunk/packages/freebsd/subversion/files/patch-subversion::libsvn_ra_dav::session.c";
            const int revision = 1398;

            // throws exception on local repository, but works on http: repositories
            svn.GetPathInfo(path, revision); 
        }

        [Test]
        public void GetYoungestRevision()
        {
            Assert.That(api.GetYoungestRevision(), Is.EqualTo(20));
        }

        [Test]
        public void GetRevisionData_Revision1_AuthorIsChristian()
        {
            var rev = api.GetRevisionData(1, 1); 
            
            Assert.That(rev, Has.Count(1));
            Assert.That(rev[0].Author, Is.EqualTo("Christian"));
            Assert.IsTrue(rev[0].Changes.TrueForAll(c => api.GetPathInfo(c.Path, c.Revision).Author == rev[0].Author));
        }

        [Test]
        public void GetRevisionData_Revision1to9_RevisionsInOrder()
        {
            var rev = api.GetRevisionData(1, 9);
            for (int i = 2; i < rev.Count; ++i)
            {
                Assert.That(rev[i - 1].Revision < rev[i].Revision);
            }
        }

        [Test]
        public void GetRevisionData_Revision9to1_RevisionsInOrder()
        {
            var rev = api.GetRevisionData(9, 1);
            for (int i = 2; i < rev.Count; ++i)
            {
                Assert.That(rev[i - 1].Revision > rev[i].Revision);
            }
        }

        [Test]
        public void GetRevisionData_Revision3_AddedPath()
        {
            var list = GetFilteredPathList(Change.Add, 3);
            Assert.That(list, Is.EquivalentTo(new[] {"/Folder/Second", "/Folder/Second/second.txt", "/Folder/text.txt"}));
        }

        [Test]
        public void GetRevisionData_Revision4_DeletedPath()
        {
            var list = GetFilteredPathList(Change.Delete, 4);
            Assert.That(list, Is.EquivalentTo(new[] {"/Folder/SubFolder/Second/second.txt"}));
        }

        [Test]
        public void GetRevisionData_Revision7_PropertiesModified()
        {
            var list = GetPathChangeList(7);
            foreach (var change in list)
            {
                Console.WriteLine(change.Path);
            }
        }

        [Test]
        public void GetRevisionData_Revision9_ModifiedPath()
        {
            var list = GetFilteredPathList(Change.Modify, 9);
            Assert.That(list, Is.EquivalentTo(new[] {"/Folder/Neuer Ordner/CopiedAndRenamed/second.txt"}));
        }

        [Test]
        public void GetRevisionData_Revision10_ReplacedPath()
        {
            var list = GetFilteredPathList(Change.Replace, 10);
            Assert.That(list, Is.EquivalentTo(new[] {"/Folder/Neuer Ordner/Second/second.txt"}));
        }

        [Test]
        public void GetRevisionData_Revision16_CopiedPath()
        {
            PathChange change = GetPathChangeList(16)[0];

            Assert.That(change.Change, Is.EqualTo(Change.Add));
            Assert.That(change.Path, Is.EqualTo("/CopyWithDeletedFolder"));
            Assert.That(change.IsCopy);
        }

        [Test]
        public void GetPathInfo_AtomicCopyWithDeleteInRev16_NoData()
        {
            string path = GetFilteredPathList(Change.Delete, 16).First();
            Assert.That(api.GetPathInfo(path, 16), Is.Null);
        }

        List<string> GetFilteredPathList(Change allowed, int revision)
        {
            var list = new List<string>();
            foreach (PathChange change in GetPathChangeList(revision))
            {
                if (change.Change == allowed) list.Add(change.Path);
            }
            return list;
        }

        List<PathChange> GetPathChangeList(int revision)
        {
            return api.GetRevisionData(revision, revision)[0].Changes;
        }

        [Test]
        public void GetPathProperties_Revision17_Properties()
        {
            var properties = api.GetPathProperties("/Folder/Second", 17);
            Assert.That(properties, Has.Count(3));
            Assert.That(properties["cr:test"], Is.EqualTo("nur ein test"));
            Assert.That(properties["cr:test2"], Is.EqualTo("another test"));
            Assert.That(properties["cr:test3"], Is.EqualTo("more tests"));
        }

        [Test]
        public void GetPathContent_Revision17_Content()
        {
            var content = api.GetPathContent("/Folder/Second/first.txt", 17, 10);
            Assert.That(content, Is.EqualTo("hullebulle"));
        }
      
        [Test]
        public void GetPathInfo_Revision17_Size()
        {
            Assert.That(api.GetPathInfo("/Folder/Second/first.txt", 17).Size, Is.EqualTo(10));
            Assert.That(api.GetPathInfo("/Folder/Second/SvnQuery.dll", 17).Size, Is.EqualTo(13312));
        }

        public Exception CatchException(Action action)
        {
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception x)
            {
                exception = x;
                Console.WriteLine(exception);
            }
            return exception;
        }

        [Test]
        public void Api_InvalidUrl_Exception()
        {
            Exception exception = CatchException(delegate
            {
                ISvnApi invalid = new SharpSvnApi("svn://bli.bla.blub");
                invalid.GetYoungestRevision();
            });
            Assert.That(exception, Is.Not.Null);
        }

        [Test]
        public void GetRevisionData_InvalidRevision_Exception()
        {
            Exception exception = CatchException(delegate { api.GetRevisionData(5000, 10000); });
            Assert.That(exception, Is.Not.Null);
        }      

    }
}