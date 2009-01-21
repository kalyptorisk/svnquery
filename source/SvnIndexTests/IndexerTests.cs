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
using System.Diagnostics;
using System.IO;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using SvnQuery;
using NUnit.Framework.Constraints;

namespace SvnIndexTests
{
    [TestFixture]
    public class IndexerTests
    {
        readonly string repository = "file:///" + Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\test_repository"));
        readonly IndexSearcher searcher;

        public IndexerTests()
        {
            //var dir = new RAMDirectory();
            //var dir = FSDirectory.GetDirectory(Path.Combine(Path.GetTempPath(), "DummyIndex")); 
            var dir = FSDirectory.GetDirectory(@"d:\testindex"); 
            Indexer indexer = new Indexer(new IndexerArgs(new []{"create", "DummyIndex", repository, "-n", "Test"}), dir);
            indexer.Run();            
            searcher = new IndexSearcher(dir);
            Debug.WriteLine(searcher.MaxDoc());
        }

        Document FindDoc(string id)
        {
            Hits h = searcher.Search(new TermQuery(new Term(FieldName.Id, id)));
            Assert.That(h.Length() == 1, id + " not found");
            return h.Doc(0);
        }

        class EqualRevisionConstraint: EqualConstraint
        {
            public EqualRevisionConstraint(int revision)
                : base(revision)
            {}

            public override bool Matches(object doc)
            {
                var d = doc as Document;
                return d != null && base.Matches(int.Parse(d.Get(FieldName.RevisionLast)));
            }
        }

        static Constraint HasLastRevision(int revision)
        {
            return new EqualRevisionConstraint(revision);
        }

        void AssertRevisions(string path, params int[] revisions)
        {
            for (int i = 1; i < revisions.Length; ++i)
            {                
                var doc = FindDoc(path + "@" + revisions[i - 1]);
                Assert.That(doc, HasLastRevision(revisions[i]));
            }
        }

        [Test, Ignore]
        public void Index_DeletedFolderNeuerOrdner_NoHeadRevisionPresent()
        {
            var doc = FindDoc("/Folder/Neuer Ordner@6");
            Assert.That(doc, HasLastRevision(18));           
        }

        [Test, Ignore]
        public void Index_FileInDeleteFolder_NoHeadRevisionPresent()
        {
            var doc = FindDoc("/Folder/Neuer Ordner/Second/first.txt@10");
            Assert.That(doc, HasLastRevision(18));            
        }      

        [Test, Ignore]
        public void Index_FileAddModifyReplace_RevisionRangeExist()
        {
            AssertRevisions("/Folder/Second/second.txt", 3, 7, 8, 17, 18, 9999999);
        }
    }
}