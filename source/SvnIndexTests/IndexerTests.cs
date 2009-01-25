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
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using SvnQuery;

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
            Indexer indexer = new Indexer(new IndexerArgs(new[] {"create", "DummyIndex", repository, "-r21", "-c3", "-n", "Test"}), dir);
            indexer.Run();
            searcher = new IndexSearcher(dir);
        }

        class Range : IComparable<Range>
        {
            public readonly int First;
            public readonly int Last;

            public Range(int first, int last)
            {
                First = first;
                Last = last;
            }

            public override bool Equals(object obj)
            {
                Range other = obj as Range;
                return other != null && First == other.First && Last == other.Last;
            }

            public override int GetHashCode()
            {
                return First ^ Last;
            }

            public int CompareTo(Range other)
            {
                return ((IComparable<int>) First).CompareTo(other.First);
            }

            public override string ToString()
            {
                return First + ":" + Last;
            }
        }

        List<Range> RevisionOrder(string path)
        {
            List<Range> results = new List<Range>();
            IndexReader r = searcher.Reader;
            path = path + "@";

            var t = r.Terms(new Term(FieldName.Id, path));            
            while (t.Term() != null && t.Term().Text().StartsWith(path))
            {
                int revisionId = int.Parse(t.Term().Text().Substring(path.Length));
                var d = r.TermDocs(t.Term());
                Assert.That(d.Next(), "document must exist:" + t.Term().Text());
                Document doc = r.Document(d.Doc());
                Assert.That(!d.Next(), "only one document can have this id: " + t.Term().Text());
                int first = int.Parse(doc.Get(FieldName.RevisionFirst));
                int last = int.Parse(doc.Get(FieldName.RevisionLast));

                Assert.That(first, Is.EqualTo(revisionId));
                results.Add(new Range(first, last));
                t.Next();
            }
            results.Sort();
            return results;
        }

        static List<Range> RevisionOrder(params int[] revisions)
        {
            List<Range> results = new List<Range>();
            for (int i = 1; i < revisions.Length; ++i)
            {
                if (revisions[i] == 0)
                {
                    if (++i + 1 == revisions.Length) throw new ArgumentException("expected at least two revisions after a zero revision");
                    continue;
                }
                int first = revisions[i - 1];
                int last = revisions[i] == -1 ? RevisionFilter.Head : revisions[i] - 1;
                results.Add(new Range(first, last));
            }
            return results;
        }

        [Test]
        public void RevisionOrder_Selftest_ContinousWithHead()
        {
            Assert.That(
                RevisionOrder(1, 5, 6, -1), 
                Is.EquivalentTo(new List<Range>{new Range(1, 4), new Range(5, 5), new Range(6, RevisionFilter.Head)}));
        }

        [Test]
        public void RevisionOrder_Selftest_NonContinous()
        {
            Assert.That(RevisionOrder(3, 5, 0, 7, 8), Is.EqualTo(new[] {new Range(3, 4), new Range(7, 7)}));
        }

        [Test]
        public void Index_HeadRevision20()
        {
            HashSet<string> headItems = new HashSet<string>();
            headItems.Add("/");
            headItems.Add("/CopyWithDeletedFolder");
            headItems.Add("/CopyWithDeletedFolder/Second");
            headItems.Add("/CopyWithDeletedFolder/Second/first.txt");
            headItems.Add("/CopyWithDeletedFolder/Second/second.txt");
            headItems.Add("/Folder");
            headItems.Add("/Folder/C#");
            headItems.Add("/Folder/Second");
            headItems.Add("/Folder/Second/SvnQuery.dll");
            headItems.Add("/Folder/Second/first.txt");
            headItems.Add("/Folder/Second/second.txt");
            headItems.Add("/Folder/Subfolder");
            headItems.Add("/Folder/Subfolder/CopiedAndRenamed");
            headItems.Add("/Folder/Subfolder/CopiedAndRenamed/second.txt");
            headItems.Add("/Folder/Subfolder/Second");
            headItems.Add("/Folder/Subfolder/Second/first.txt");
            headItems.Add("/Folder/Subfolder/Second/second.txt");
            headItems.Add("/Folder/import");
            headItems.Add("/Folder/text.txt");

            Hits hits = searcher.Search(new TermQuery(new Term(FieldName.RevisionLast, RevisionFilter.HeadString)));
            for (int i = 0; i < hits.Length(); ++i)
            {
                string id = hits.Doc(i).Get(FieldName.Id).Split('@')[0];
                Assert.That(headItems.Contains(id), id + " should be in head revision");
                headItems.Remove(id);
            }
            Assert.That(headItems, Has.Count(0));
        }

        [Test]
        public void Index_FolderSecondSecondTxt_ContinousRevisionOrder()
        {
            Assert.That(
                RevisionOrder("/Folder/Second/second.txt"), 
                Is.EquivalentTo(RevisionOrder(3, 8, 18, -1)));
        }

        [Test]
        public void Index_CopiedAndRenamed_RevisionOrder()
        {
            Assert.That(
                RevisionOrder("/Folder/Neuer Ordner/CopiedAndRenamed"),
                Is.EquivalentTo(RevisionOrder(6, 19)));
            Assert.That(
               RevisionOrder("/Folder/Neuer Ordner/CopiedAndRenamed/second.txt"),
               Is.EquivalentTo(RevisionOrder(6, 9, 19)));
        }

        [Test]
        public void Index_FolderTextTxt_NonContinousRevisionOrder()
        {
            Assert.That(
                RevisionOrder("/Folder/text.txt"),
                Is.EquivalentTo(RevisionOrder(3, 11, 0, 21, -1)));
        }

        [Test]
        public void Index_MessageContainsBinary_ExpectedResults()
        {
            Parser p = new Parser(searcher.Reader);
            Hits h = searcher.Search(p.Parse("m:binary"));
            Assert.That(h.Length(), Is.EqualTo(4));
            var expected = new HashSet<string>();
            expected.Add("$Revision@17");
            expected.Add("/Folder/Second@17");
            expected.Add("/Folder/import@17");
            expected.Add("/Folder/Second/SvnQuery.dll@17");
            for (int i = 0; i < h.Length(); i++)
            {
                Assert.That(expected, Has.Member(h.Doc(i).Get(FieldName.Id)));
            }

        }
    
    }
}