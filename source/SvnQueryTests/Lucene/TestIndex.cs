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
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using SvnQuery.Lucene;
using Directory = Lucene.Net.Store.Directory;

namespace SvnQuery.Tests.Lucene
{
    public static class TestIndex
    {
        const string LongContentA = "#include \"FileIO.h\" // another comment that is just here to generate some text that can be searched";
        const string LongContentB = "#include <general/path/bla.h> // some very funky and long comment that is total useless";

        static readonly string[,] Data = {
                                             {" 0", "/csharp/fileio.cs", "The quick brown fox jumps over the lazy dog", ""},
                                             {" 1", "/shared/general/FileIO/FileIO.cpp", "class Special:Abstract", ""},
                                             {" 2", "/shared/general/FileIO/FileIO.design.cpp", "obj.method(arg1, arg2, arg3) < 4711", ""},
                                             {" 3", "/shared/general/FileIO/FileIO.h", "aa bb cc dd ee ff ee dd cc bb aa aa bb cc dd", ""},
                                             {" 4", "/shared/general/FileIO/FileIO.xml", "aa bb cc dd cc", ""},
                                             {" 5", "/shared/general/flip/FileIO/FileIO.h", "cc dd ee ff", ""},
                                             {" 6", "/shared/general/FileIO/anders.h", "flip fileio cpp shared general", ""},
                                             {" 7", "/shared/general/FileIO/anders.cpp", "", ""},
                                             {" 8", "/tags/shared/general/FileIO/FileIO.cpp", LongContentA, ""},
                                             {" 9", "/tags/shared/general/FileIO/FileIO.h", LongContentB, ""},
                                             {"10", "/tags/shared/general/FileIO/anders.h", "max und moritz sind anders", ""},
                                             {"11", "/tags/shared/general/FileIO/anders.cpp", "anders sind moritz und max", ""},
                                             {"12", "/woanders/FileIO.cpp", "elephant", ""},
                                             {"13", "/woanders/FileIO.h", "cat and mice", ""},
                                             {"14", "/woanders/flip.cs", "cat and dog", ""},
                                             {"15", "/woanders/FileIO/hier/und/dort/fileio.cpp", "elefant, cat, dog, mice, rabbit, hedgehog and", "-r4000 /bla/bli localbli"},
                                             {"16", "/woanders/FileIO/hier/und/dort/fileio.h", "Elefant Katze Maus Hase Igel", "-r5000 ^/products/internal internal"},
                                             {"17", "/selt.sam/source/form1.design.cs", "Der Elefant sitzt auf dem Trommelklo", "svn://svnquery.tigris.org/one/two/three localfolder"},
                                             {"18", "/woanders/", "", "/shared shared"},
                                             {"19", "/project/import/", "", "/Shared/General general\r\n  /woanders woanders"},
                                             {"20", "/project_zwei/import/", "", "/Shared/Animals animals"},
                                             // special revision entries start with /revision, if so the next two fields are firtst rev and alst rev
                                             {"21", "/revisions/bla.cpp", "1", "5"},
                                             {"22", "/revisions/bla.cpp", "6", "6"},
                                             {"23", "/revisions/bla.cpp", "7", "8"},
                                             {"24", "/revisions/bla.cpp", "9", null},
                                             {"25", "/revisions/deleted.cpp", "6", "8"},
                                             {"26", "/revisions/current.cpp", "9", null},
                                             // large data
                                             {"27", "/large/data.txt", GenerateLargeContent(), ""}
                                         };

        public const int MaxNumberOfTermsPerDocument = 50;

        static string GenerateLargeContent()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < MaxNumberOfTermsPerDocument + 1; ++i)
            {
                sb.Append('a');
                sb.Append(i);
                sb.Append(' ');
            }            
            return sb.ToString();
        }

        static readonly IndexSearcher Searcher;

        public static IndexReader Reader
        {
            get { return Searcher.Reader; }
        }

        public static string GetId(int i)
        {
            return Data[i, 1];
        }

        public static string GetPath(int i)
        {
            return Data[i, 1];
        }

        public static string GetContent(int i)
        {
            return Data[i, 2];
        }

        public static string GetExternals(int i)
        {
            return Data[i, 3];
        }

        public static Hits Search(Query q, int revFirst, int revLast)
        {
            return Searcher.Search(q, new RevisionFilter(revFirst, revLast));
        }

        public static Hits SearchHeadRevision(Query q)
        {
            return Search(q, Revision.Head, Revision.Head);
        }

        public static void AssertQuery(Query q, params int[] expected)
        {
            AssertHitsAreOK(SearchHeadRevision(q), expected);
        }

        public static void AssertQueryFromHeadRevision(string query, params int[] expected)
        {
            AssertQueryFromRevisionRange(Revision.Head, Revision.Head, query, expected);
        }

        public static void AssertQueryFromRevision(int revision, string query, params int[] expected)
        {
            AssertQueryFromRevisionRange(revision, revision, query, expected);
        }

        public static void AssertQueryFromRevisionRange(int revFirst, int revLast, string query, params int[] expected)
        {
            Parser p = new Parser(Reader);
            AssertHitsAreOK(Search(p.Parse(query), revFirst, revLast), expected);
        }

        public static void PrintHits(string query)
        {
            PrintHits(SearchHeadRevision(new Parser(Reader).Parse(query)));
        }

        public static void PrintHits(Query query)
        {
            PrintHits(SearchHeadRevision(query));
        }

        public static void PrintHits(Hits hits)
        {
            if (hits == null)
            {
                Console.WriteLine("Hits: 0");
            }
            else
            {
                Console.WriteLine("Hits: " + hits.Length());

                // iterate over the first few results.
                for (int i = 0; i < 50 && i < hits.Length(); i++)
                {
                    var doc = hits.Doc(i);
                    string id = doc.Get("id");
                    int n;
                    for (n = 0; n < Data.GetLength(0); ++n)
                    {
                        if (id == Data[n, 1])
                        {
                            Console.WriteLine("{0}: {1}  ==>  {2} {3}", n, GetPath(n), GetContent(n), GetExternals(n));
                            break;
                        }
                    }
                }
                Console.WriteLine("-");
            }
        }

        public static void AssertHitsAreOK(Hits hits, params int[] expectedIndex)
        {
            PrintHits(hits);
            Assert.AreEqual(expectedIndex.Length, hits.Length(), "Incorrect number of hits");
            for (int i = 0; i < hits.Length(); ++i)
            {
                string path = hits.Doc(i).Get("id");
                bool found = false;
                for (int ii = 0; ii < expectedIndex.Length; ++ii)
                {
                    if (path == GetId(expectedIndex[ii]))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Assert.Fail("Unexpected hit: " + path);
                }
            }
        }

        static string RevisionFieldValue(int revision)
        {
            return revision.ToString(RevisionFilter.RevFormat);
        }

        static string HeadRevisionFieldValue()
        {
            return Revision.HeadString;
        }

        static TestIndex()
        {
            Directory directory = new RAMDirectory();
            IndexWriter writer = new IndexWriter(directory, null, true);
            writer.SetMaxFieldLength(MaxNumberOfTermsPerDocument);
            var pathTokenStream = new PathTokenStream();
            var contentTokenStream = new SimpleTokenStream();
            var externalsTokenStream = new PathTokenStream();
            Field field_id = new Field("id", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
            Field field_rev_first = new Field(FieldName.RevisionFirst, "", Field.Store.NO, Field.Index.UN_TOKENIZED);
            Field field_rev_last = new Field(FieldName.RevisionLast, "", Field.Store.NO, Field.Index.UN_TOKENIZED);
            Document doc = new Document();
            doc.Add(field_id);
            doc.Add(new Field(FieldName.Path, pathTokenStream));
            doc.Add(new Field(FieldName.Content, contentTokenStream));
            doc.Add(new Field(FieldName.Externals, externalsTokenStream));
            doc.Add(field_rev_first);
            doc.Add(field_rev_last);
            for (int i = 0; i < Data.GetLength(0); ++i)
            {
                string id = Data[i, 1];
                field_id.SetValue(id);
                pathTokenStream.Text = id;
                int rev_first = Revision.Head;
                if (id.StartsWith("/revisions"))
                {
                    contentTokenStream.Text = "";
                    externalsTokenStream.Text = "";
                    rev_first = int.Parse(Data[i, 2]);
                }
                else
                {
                    contentTokenStream.Text = Data[i, 2];
                    externalsTokenStream.Text = Data[i, 3];
                }
                field_rev_first.SetValue(RevisionFieldValue(rev_first));
                field_rev_last.SetValue(HeadRevisionFieldValue());
                writer.AddDocument(doc);

                if (id.StartsWith("/revisions") && Data[i, 3] != null) // update last revision
                {
                    // Change the last revision
                    // Warning: It is not possible to load a document from the index
                    // We have to rebuild/reparse it from the scratch
                    writer.DeleteDocuments(new Term("id", id));
                    pathTokenStream.Text = id;
                    contentTokenStream.Text = "";
                    externalsTokenStream.Text = "";
                    int rev_last = int.Parse(Data[i, 3]);
                    field_rev_last.SetValue(RevisionFieldValue(rev_last));
                    id += "@" + rev_first;
                    Data[i, 1] = id;
                    field_id.SetValue(id);
                    writer.AddDocument(doc);
                }
            }

            // delete non existent document test
            writer.DeleteDocuments(new Term("id", "bliflaiwj123dj33"));

            writer.Optimize();
            writer.Close();

            Searcher = new IndexSearcher(directory);
            Assert.AreEqual(Data.GetLength(0), Searcher.MaxDoc()); // smoke test for index creation 
        }
    }
}