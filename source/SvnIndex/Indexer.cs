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
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace SvnQuery
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Because of a svnlook constraing in history max revision is constrained to 8 digits decimals 99999999
    /// </remarks>
    public class Indexer
    {
        const int headRevision = 99999999; // the longest revision svnlook is able to produce

        readonly HashSet<string> createdDocs = new HashSet<string>();
        readonly HashSet<string> deletedDocs = new HashSet<string>();
        readonly SvnLookProcessor svnlook = new SvnLookProcessor();
        readonly SvnPathInfoReader svninfo;
        readonly string index;
        readonly string repository;
        int revision;

        IndexWriter indexWriter;
        int processedDocCount;
        bool treeWalking;

        // reused objects for faster indexing
        readonly Document doc = new Document();
        readonly Term idTerm = new Term("id", "");
        readonly Field idField = new Field("id", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field revFirstField = new Field("rev_first", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field revLastField = new Field("rev_last", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field sizeField = new Field("size", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field timestampField = new Field("timestamp", "", Field.Store.YES, Field.Index.NO);
        readonly Field authorField = new Field("author", "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field contentField;
        readonly Field externalsField;
        readonly PathTokenStream pathTokenStream;
        readonly ContentFromRepository contentTokenStream;
        readonly ExternalsFromRepository externalsTokenStream;

        public Indexer(string index, string repository, string revision)
        {
            this.index = index;
            this.repository = repository;

            int youngest = 0;
            svnlook.Run("youngest " + repository, r => youngest = int.Parse(r));
            this.revision = Math.Min(int.Parse(revision), youngest);

            svninfo = new SvnPathInfoReader(repository);

            doc = new Document();
            pathTokenStream = new PathTokenStream("");
            contentTokenStream = new ContentFromRepository(repository);
            externalsTokenStream = new ExternalsFromRepository(repository);
            contentField = new Field("content", contentTokenStream);
            externalsField = new Field("externals", externalsTokenStream);
            idTerm = new Term("id", "");
            doc.Add(idField);
            doc.Add(new Field("path", pathTokenStream));
            doc.Add(revFirstField);
            doc.Add(revLastField);
            doc.Add(timestampField);
            doc.Add(authorField);
        }

        public void CreateIndex()
        {
            Console.WriteLine("Create index ...");
            indexWriter = new IndexWriter(FSDirectory.GetDirectory(index), true, new StandardAnalyzer(), true);
            WalkRevisions(1, revision);
            indexWriter.Optimize();
            indexWriter.Close();
            indexWriter = null;
            Console.WriteLine("Index created from revision 1 to " + revision);
        }

        public void UpdateIndex()
        {
            int startRevision = MaxIndexRevision.Get(index) + 1;
            if (startRevision > revision)
            {
                Console.WriteLine("Index is up to date");
            }
            else
            {
                Console.WriteLine("Update index ...");
                indexWriter = new IndexWriter(FSDirectory.GetDirectory(index), false, new StandardAnalyzer(), false);
                WalkRevisions(startRevision, revision);
                if (revision % 25 == 0 || revision - startRevision > 5)
                    indexWriter.Optimize();
                indexWriter.Close();
                indexWriter = null;
                Console.WriteLine("Index updated from revision {0} to {1}", startRevision, revision);
            }
        }

        void WalkRevisions(int startRevision, int stopRevision)
        {
            processedDocCount = 0;
            indexWriter.SetRAMBufferSizeMB(32);
            for (revision = startRevision; revision <= stopRevision; ++revision)
            {
                createdDocs.Clear();
                deletedDocs.Clear();
                svnlook.Run("changed " + repository + " -r" + revision, ProcessChangeInfo);
            }
        }

        void ProcessChangeInfo(string change_plus_path)
        {
            //if (revision == 13) Debugger.Break();

            if (change_plus_path == null || change_plus_path.Length <= 4) return;
            string path = change_plus_path.Substring(4);
            if (path[0] != '/') path = "/" + path;
            char action = change_plus_path[0];
            switch (action)
            {
                case 'A':
                    CreateDocument(path);
                    break;
                case '_':
                case 'U':
                    UpdateDocument(path);
                    break;
                case 'D':
                    DeleteDocument(path);
                    break;
                default:
                    throw new NotImplementedException("Unknown action " + action);
            }
        }

        static bool IsValidPath(string path)
        {
            return !string.IsNullOrEmpty(path) && !path.ToLowerInvariant().Contains("/tags/");
        }

        void PrintProgress(char action, string path)
        {
            Console.Write(action);
            Console.Write((++processedDocCount).ToString().PadLeft(8));
            Console.Write(revision.ToString().PadLeft(8));
            Console.Write(" ");
            Console.WriteLine(path);
        }

        void CreateDocument(string path)
        {
            if (!IsValidPath(path) || createdDocs.Contains(path)) return;
            createdDocs.Add(path);

            if (path[path.Length - 1] == '/' && !treeWalking)
            {
                treeWalking = true;
                svnlook.Run("tree " + repository + " \"" + path + "\" --full-paths -r" + revision, CreateDocument); // need "/" to prefix pathes            
                treeWalking = false;
            }
            PrintProgress('A', path);
            AddDocument(svninfo.ReadPathInfo(path, revision), headRevision);
        }

        void UpdateDocument(string path)
        {
            if (!IsValidPath(path)) return;
            PrintProgress('U', path);
            SvnPathInfo info = svninfo.ReadPathInfo(path, revision - 1);
            if (info != null) // atomic replace or copy and modify
            {
                indexWriter.DeleteDocuments(idTerm.CreateTerm(path));
                AddDocument(info, revision - 1);
            }
            AddDocument(svninfo.ReadPathInfo(path, revision), headRevision);
        }

        void DeleteDocument(string path)
        {
            if (!IsValidPath(path) || deletedDocs.Contains(path)) return;

            SvnPathInfo pathInfo = svninfo.ReadPathInfo(path, revision - 1);
            if (pathInfo == null) return; // atomic replace

            if (path[path.Length - 1] == '/' && !treeWalking)
            {
                treeWalking = true;
                svnlook.Run("tree " + repository + " \"" + path + "\" --full-paths -r" + (revision - 1), DeleteDocument); // need "/" to prefix pathes            
                treeWalking = false;
            }

            PrintProgress('D', path);
            indexWriter.DeleteDocuments(idTerm.CreateTerm(path));
            deletedDocs.Add(path);
            AddDocument(pathInfo, revision - 1);
        }

        void AddDocument(SvnPathInfo info, int revLast)
        {
            idField.SetValue(revLast == headRevision ? info.path : info.path + ":" + info.revision);
            pathTokenStream.Reset(info.path);
            revFirstField.SetValue(info.revision.ToString("d8"));
            revLastField.SetValue(revLast.ToString("d8"));
            authorField.SetValue(info.author);
            timestampField.SetValue(info.timestamp.ToString("yyyy-MM-dd hh:mm"));

            doc.RemoveFields(sizeField.Name());
            if (info.size >= 0)
            {
                sizeField.SetValue(PackedSizeConverter.ToSortableString(info.size));
                doc.Add(sizeField);
            }

            string revisionString = info.revision.ToString();

            doc.RemoveFields(contentField.Name());
            if (contentTokenStream.Reset(info.path, revisionString))
                doc.Add(contentField);

            doc.RemoveFields(externalsField.Name());
            if (externalsTokenStream.Reset(info.path, revisionString))
                doc.Add(externalsField);

            indexWriter.AddDocument(doc);
        }
    }
}