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
using SvnIndex;
using SvnQuery.Lucene;

namespace SvnIndexTests
{
    [TestFixture]
    public class IndexerTests
    {
        readonly string _repository = "file:///" + Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\test_repository"));
        IndexSearcher _revision22;

        IndexSearcher Revision22
        {
            get
            {
                if (_revision22 == null) _revision22 = new IndexSearcher(CreateIndex(22));
                return _revision22;
            }
        }

        RAMDirectory CreateIndex(int revision)
        {
            var dir = new RAMDirectory();
            Indexer indexer = new Indexer(new IndexerArgs(new[] { "create", "RAMDirectory", _repository, "-r", revision.ToString(), "-c3", "-n", "Test", "-v4" }), dir);
            indexer.Run();
            return dir;
        }

        RAMDirectory CreateSingleRevisionIndex(int revision)
        {
            var dir = new RAMDirectory();
            Indexer indexer = new Indexer(new IndexerArgs(new[] { "create", "RAMDirectory", _repository, "-r", revision.ToString(), "-s", "-v4" }), dir);
            indexer.Run();
            return dir;
        }

        void UpdateSingleRevisionIndex(int revision, RAMDirectory dir)
        {
            Indexer indexer = new Indexer(new IndexerArgs(new[] { "update", "RAMDirectory", _repository, "-r", revision.ToString(), "-s", "-v4" }), dir);
            indexer.Run();
        }

        class Range : IComparable<Range>
        {
            readonly int _first;
            readonly int _last;

            public Range(int first, int last)
            {
                _first = first;
                _last = last;
            }

            public override bool Equals(object obj)
            {
                Range other = obj as Range;
                return other != null && _first == other._first && _last == other._last;
            }

            public override int GetHashCode()
            {
                return _first ^ _last;
            }

            public int CompareTo(Range other)
            {
                return ((IComparable<int>) _first).CompareTo(other._first);
            }

            public override string ToString()
            {
                return _first + ":" + _last;
            }
        }

        static List<Range> RevisionOrder(string path, IndexSearcher index)
        {
            List<Range> results = new List<Range>();
            IndexReader r = index.Reader;
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

        static void CheckHeadRevision22(IndexSearcher searcher)
        {
            var headItems = new HashSet<string>
                                {
                                    "/",
                                    "/CopyWithDeletedFolder",
                                    "/CopyWithDeletedFolder/Second",
                                    "/CopyWithDeletedFolder/Second/first.txt",
                                    "/CopyWithDeletedFolder/Second/second.txt",
                                    "/Folder",
                                    "/Folder/C#",
                                    "/Folder/Second",
                                    "/Folder/Second/SvnQuery.dll",
                                    "/Folder/Second/first.txt",
                                    "/Folder/Second/second.txt",
                                    "/Folder/Subfolder",
                                    "/Folder/Subfolder/CopiedAndRenamed",
                                    "/Folder/Subfolder/CopiedAndRenamed/second.txt",
                                    "/Folder/Subfolder/Second",
                                    "/Folder/Subfolder/Second/first.txt",
                                    "/Folder/Subfolder/Second/second.txt",
                                    "/Folder/import",
                                    "/Folder/text.txt",
                                    "/tags",
                                };
            
            Hits hits = searcher.Search(new TermQuery(new Term(FieldName.RevisionLast, RevisionFilter.HeadString)));
            for (int i = 0; i < hits.Length(); ++i)
            {
                string id = hits.Doc(i).Get(FieldName.Id).Split('@')[0];
                Assert.That(headItems.Contains(id), id + " is in head revision but shouldn't");
                headItems.Remove(id);
            }
            Assert.AreEqual(0, headItems.Count);            
        }

        static void CheckIsHeadOnly(IndexSearcher searcher)
        {
            TermEnum t = searcher.Reader.Terms(new Term(FieldName.RevisionLast, "0"));
            Assert.IsNotNull(t);
            Assert.AreEqual(FieldName.RevisionLast, t.Term().Field());
            while (t.Term().Field() == FieldName.RevisionLast)
            {
                Assert.AreEqual(RevisionFilter.HeadString, t.Term().Text());
                if (t.Next()) continue;
            }
        }

        [Test]
        public void Index_HeadRevision22()
        {
            CheckHeadRevision22(Revision22);
        }

        [Test]
        public void IndexSingleRevision_HeadRevision22()
        {
            var index = new IndexSearcher(CreateSingleRevisionIndex(22));
            CheckIsHeadOnly(index);
            CheckHeadRevision22(index);
        }

        [Test]
        public void UpdateIndexSingleRevision_HeadRevision22()
        {
            RAMDirectory dir = CreateSingleRevisionIndex(7);
            UpdateSingleRevisionIndex(22, dir);
            var index = new IndexSearcher(dir);
            CheckIsHeadOnly(index);
            CheckHeadRevision22(index);
        }

        [Test]
        public void Index_FolderSecondSecondTxt_ContinousRevisionOrder()
        {
            Assert.That(
                RevisionOrder("/Folder/Second/second.txt", Revision22), 
                Is.EquivalentTo(RevisionOrder(3, 8, 18, -1)));
        }

        [Test]
        public void Index_CopiedAndRenamed_RevisionOrder()
        {
            Assert.That(
                RevisionOrder("/Folder/Neuer Ordner/CopiedAndRenamed", Revision22),
                Is.EquivalentTo(RevisionOrder(6, 19)));
            Assert.That(
               RevisionOrder("/Folder/Neuer Ordner/CopiedAndRenamed/second.txt", Revision22),
               Is.EquivalentTo(RevisionOrder(6, 9, 19)));
        }

        [Test]
        public void Index_FolderTextTxt_NonContinousRevisionOrder()
        {
            Assert.That(
                RevisionOrder("/Folder/text.txt", Revision22),
                Is.EquivalentTo(RevisionOrder(3, 11, 0, 21, -1)));
        }

        [Test]
        public void Index_MessageContainsBinary_ExpectedResults()
        {
            Parser p = new Parser(Revision22.Reader);
            Hits h = Revision22.Search(p.Parse("m:binary"));
            Assert.That(h.Length(), Is.EqualTo(4));
            var expected = new HashSet<string>
                           {
                               "$Revision 17",
                               "/Folder/Second@17",
                               "/Folder/import@17",
                               "/Folder/Second/SvnQuery.dll@17"
                           };
            for (int i = 0; i < h.Length(); i++)
            {
                Assert.That(expected, Has.Member(h.Doc(i).Get(FieldName.Id)));
            }

        }
    
    }
}