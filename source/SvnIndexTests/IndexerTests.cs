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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using SvnQuery;
using NUnit.Framework.Constraints;
using NUnit.Framework.SyntaxHelpers;

namespace SvnIndexTests
{

    [TestFixture]
    public class IndexerTests
    {
        readonly string repository = "file:///" + Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\test_repository"));
        readonly IndexSearcher searcher;

        public IndexerTests()
        {
            var dir = new RAMDirectory();
            //var dir = FSDirectory.GetDirectory(Path.Combine(Path.GetTempPath(), "DummyIndex")); 
            //var dir = FSDirectory.GetDirectory(@"d:\testindex"); 
            Indexer indexer = new Indexer(new IndexerArgs(new[] { "create", "DummyIndex", repository, "-n", "Test" }), dir);
            indexer.Run();
            searcher = new IndexSearcher(dir);
        }

        Document FindDoc(string id)
        {
            Hits h = searcher.Search(new TermQuery(new Term(FieldName.Id, id)));
            Assert.That(h.Length() == 1, id + " not found");
            return h.Doc(0);
        }

        class RevisionRange: IComparable<RevisionRange>
        {
            public readonly int First;
            public readonly int Last;

            public RevisionRange(int first, int last)
            {
                First = first;
                Last = last;
            }

            public override bool Equals(object obj)
            {
                RevisionRange other = obj as RevisionRange;
                return other!= null && First == other.First && Last == other.Last;
            }

            public override int GetHashCode()
            {
                return First ^ Last;
            }

            public int CompareTo(RevisionRange other)
            {
                return ((IComparable<int>) First).CompareTo(other.First); 
            }

            public override string ToString()
            {
                return First + ":" + Last;
            }
        }

        List<RevisionRange> RevisionOrder(string path)
        {
            List<RevisionRange> results = new List<RevisionRange>();
            IndexReader r = searcher.Reader;
            path = path + "@";
            var t = r.Terms(new Term(FieldName.Id, path));
            while (t.Next())
            {
                if (!t.Term().Text().StartsWith(path)) break;

                int revisionId = int.Parse(t.Term().Text().Substring(path.Length));
                var d = r.TermDocs(t.Term());
                while (d.Next())
                {
                    Document doc = r.Document(d.Doc());
                    int first = int.Parse(doc.Get(FieldName.RevisionFirst));
                    int last = int.Parse(doc.Get(FieldName.RevisionLast));

                    Assert.That(first, Is.EqualTo(revisionId));
                    results.Add(new RevisionRange (first, last));
                }
            }
            results.Sort();
            return results;
        }

        static List<RevisionRange> RevisionOrder(params int[] revisions)
        {
            List<RevisionRange> results = new List<RevisionRange>();
            for (int i = 1; i < revisions.Length; ++i)
            {
                if (revisions[i] == 0) 
                {
                    if (++i + 1 == revisions.Length) throw new ArgumentException("expected at least two revisions after a zero revision");
                    continue; 
                }
                results.Add(new RevisionRange(revisions[i - 1], revisions[i] == -1 ? 99999999 : revisions[i] - 1));
            }
            return results;
        }

        [Test]
        public void RevisionOrder_ContinousWithHead()
        {
            Assert.That(RevisionOrder(1, 5, 6, -1), 
                Is.EquivalentTo(new List<RevisionRange>{new RevisionRange(1, 4), new RevisionRange(5, 5), new RevisionRange(6, 99999999)}));
        }

        [Test]
        public void RevisionOrder_NonContinous()
        {
            Assert.That(RevisionOrder(3, 5, 0, 7, 8), Is.EqualTo(new[] { new RevisionRange(3, 4), new RevisionRange(7, 7)}));
        }

        //void AssertRevisions(string path, params int[] revisions)
        //{
        //    for (int i = 1; i < revisions.Length; ++i)
        //    {                
        //        var doc = FindDoc(path + "@" + revisions[i - 1]);
        //        Assert.That(doc, HasLastRevision(revisions[i]));
        //    }
        //}

        //[Test, Ignore]
        //public void Index_DeletedFolderNeuerOrdner_NoHeadRevisionPresent()
        //{
        //    var doc = FindDoc("/Folder/Neuer Ordner@6");
        //    Assert.That(doc, HasLastRevision(18));           
        //}

        //[Test, Ignore]
        //public void Index_FileInDeleteFolder_NoHeadRevisionPresent()
        //{
        //    var doc = FindDoc("/Folder/Neuer Ordner/Second/first.txt@10");
        //    Assert.That(doc, HasLastRevision(18));            
        //}      

        [Test]
        public void Index_FileAddModifyReplace_RevisionRangeExist()
        {

            Assert.That(RevisionOrder("/Folder/Second/second.txt"), Is.EquivalentTo(RevisionOrder(3, 7, 8, 17, 18, 9999999)));
        }

        // Tests
        // Non continous RevsionOrder
        // CopiedPath
        // DeletedPath
        // HeadRevision
        // AtMaxOneHeadRevisionPerPath

//       @" CopyWithDeletedFolder/
//CopyWithDeletedFolder/Second/
//CopyWithDeletedFolder/Second/first.txt
//CopyWithDeletedFolder/Second/second.txt
//Folder/
//Folder/C#/
//Folder/Second/
//Folder/Second/SvnQuery.dll
//Folder/Second/first.txt
//Folder/Second/second.txt
//Folder/Subfolder/
//Folder/Subfolder/CopiedAndRenamed/
//Folder/Subfolder/CopiedAndRenamed/second.txt
//Folder/Subfolder/Second/
//Folder/Subfolder/Second/first.txt
//Folder/Subfolder/Second/second.txt
//Folder/import/
//šml„ute ”„/
//šml„ute ”„/bla#[1]{}.txt"
    }
}