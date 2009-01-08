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
using System.IO;
using System.Collections.Generic;
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
            repository = "file:///" + Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\test_repository"));
            //repository = "svn://localhost/";
        }

        [Test]        
        public void GetYoungestRevision()
        {                       
            Assert.That(api.GetYoungestRevision(), Is.EqualTo(18));
        }

        [Test]
        public void ForEachChange_Revision1_AuthorIsChristian()
        {
            var list = new List<string>();
            api.ForEachChange(1, 1, change => list.Add(api.GetPathData(change.Path, change.Revision).Author));
            Assert.That(list, Has.All.EqualTo("Christian"));
        }

        [Test]
        public void ForEachChange_Revision1to9_RevisionsInOrder()
        {
            var list = new List<int>();
            api.ForEachChange(1, 9, change => list.Add(change.Revision));
            for (int i = 2; i < list.Count; ++i)
            {
                Assert.That(list[i - 1] <= list[i]);
            }
        }

        [Test]
        public void ForEachChange_Revision9to1_RevisionsInOrder()
        {
            var list = new List<int>();
            api.ForEachChange(9, 1, change => list.Add(change.Revision));
            for (int i = 2; i < list.Count; ++i)
            {
                Assert.That(list[i - 1] >= list[i]);
            }
        }

        [Test]
        public void ForEachChange_Revision3_AddedPath()
        {
            var list = GetFilteredPathList(Change.Add, 3);            
            Assert.That(list, Is.EquivalentTo(new []{"/Folder/Second", "/Folder/Second/second.txt", "/Folder/text.txt"}));
        }

        [Test]
        public void ForEachChange_Revision4_DeletedPath()
        {
            var list = GetFilteredPathList(Change.Delete, 4);
            Assert.That(list, Is.EquivalentTo(new[] {"/Folder/SubFolder/Second/second.txt"}));            
        }

        [Test]
        public void ForEachChange_Revision7_PropertiesModified()
        {
            var list = GetPathChangeList(7);
            foreach (var change in list)
            {
                Console.WriteLine(change.Path);
            }
        }

        [Test]
        public void ForEachChange_Revision9_ModifiedPath()
        {
            var list = GetFilteredPathList(Change.Modify, 9);
            Assert.That(list, Is.EquivalentTo(new []{"/Folder/Neuer Ordner/CopiedAndRenamed/second.txt"}));            
        }

        [Test]
        public void ForEachChange_Revision10_ReplacedPath()
        {
            var list = GetFilteredPathList(Change.Replace, 10);
            Assert.That(list, Is.EquivalentTo(new[] { "/Folder/Neuer Ordner/Second/second.txt" }));
        }

        [Test]
        public void ForEachChange_Revision16_CopiedPath()
        {
            PathChange change = GetPathChangeList(16)[0];

            Assert.That(change.Change, Is.EqualTo(Change.Add));
            Assert.That(change.Path, Is.EqualTo("/CopyWithDeletedFolder"));
            Assert.That(change.IsCopy);
        }

        [Test]
        public void GetPathData_AtomicCopyWithDeleteInRev16_NoData()
        {
            string path = GetFilteredPathList(Change.Delete, 16).First();
            Assert.That(api.GetPathData(path, 16), Is.Null);
        }

        List<PathChange> GetPathChangeList(int revision)
        {
            var list = new List<PathChange>();
            api.ForEachChange(revision, revision, list.Add);
            return list;
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

        [Test]
        public void GetPathData_Revision17_Properties()
        {
            PathData data = api.GetPathData("/Folder/Second", 17);
            Assert.That(data.Properties, Has.Count(3));
            Assert.That(data.Properties["cr:test"], Is.EqualTo("Nur ein Test"));
            Assert.That(data.Properties["cr:test2"], Is.EqualTo("Another test"));
            Assert.That(data.Properties["cr:test3"], Is.EqualTo("more tests"));            
        }

        [Test]
        public void GetPathData_Revision17_Content()
        {
            PathData data = api.GetPathData("/Folder/Second/first.txt", 17);
            Assert.That(data.Text, Is.EqualTo("hullebulle"));            
        }

        [Test]
        public void GetPathData_Revision17_BinaryHasNoText()
        {
            PathData data = api.GetPathData("/Folder/Second/SvnQuery.dll", 17);

            Assert.That(data.Properties["svn:mime-type"], Is.Not.StartsWith("text/"));
            Assert.That(data.Text, Is.Null);            
        }

        [Test]
        public void GetPathData_Revision17_Size()
        {
            Assert.That(api.GetPathData("/Folder/Second/first.txt", 17).Size, Is.EqualTo(10));
            Assert.That(api.GetPathData("/Folder/Second/SvnQuery.dll", 17).Size, Is.EqualTo(13312));            
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
        public void ForEachChange_InvalidRevision_Exception()
        {
            Exception exception = CatchException(delegate
            {
                api.ForEachChange(5000, 10000, delegate {});
            });
            Assert.That(exception, Is.Not.Null);
        }


    }
}