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
using System.Text.RegularExpressions;
using System.Threading;
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

        readonly IndexerArgs args;

        readonly string index;
        readonly string repository;
        readonly ISvnApi svn;

        readonly FinalizedDictionary finalized = new FinalizedDictionary();
        readonly PendingReads pendingReads = new PendingReads();

        Thread indexThread;
        IndexWriter indexWriter;
        readonly Queue<PathData> indexQueue = new Queue<PathData>();
        readonly EventWaitHandle indexQueueHasData = new ManualResetEvent(false);
        readonly Semaphore indexQueueLimit;
        int indexedDocuments;


        readonly HashSet<string> createdDocs = new HashSet<string>();
        readonly HashSet<string> deletedDocs = new HashSet<string>();
        readonly SvnLookProcessor svnlook = new SvnLookProcessor();
        readonly SvnPathInfoReader svninfo;
        int obsolete_global_revision;
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

        public enum Command
        {
            Create,
            Update
        } ;

        public Indexer(IndexerArgs args)
        {
            this.args = args;
            index = args.IndexPath;
            repository = args.RepositoryUri;
            indexQueueLimit = new Semaphore(args.MaxThreads + 1, args.MaxThreads + 1);
            ThreadPool.SetMaxThreads(args.MaxThreads + Environment.ProcessorCount + 1, 1000);

            svn = new SharpSvnApi(repository, args.User, args.Password);

            obsolete_global_revision = args.MaxRevision = Math.Min(args.MaxRevision, svn.GetYoungestRevision());

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

        public void Run()
        {
            bool create = args.Command == Command.Create;
            int startRevision = create ? 1 : MaxIndexRevision.Get(index) + 1;
            int stopRevision = args.MaxRevision;

            Console.WriteLine("Indexing started for " + startRevision + " to " + stopRevision);

            indexWriter = new IndexWriter(FSDirectory.GetDirectory(args.IndexPath), create, new StandardAnalyzer(), create);
            indexWriter.SetRAMBufferSizeMB(32);

            // reverse order to minimize document updates 
            indexThread = new Thread(IndexThread);
            indexThread.Start();
            pendingReads.Increment();
            svn.ForEachChange(stopRevision, startRevision, QueueChange);
            pendingReads.Decrement();
            indexThread.Join();

            //WalkRevisions(startRevision, revision);

            if (create || stopRevision % args.Optimise == 0 || stopRevision - startRevision > args.Optimise)
                indexWriter.Optimize();
            indexWriter.Close();
            indexWriter = null;
            Console.WriteLine("Indexing finished for " + startRevision + " to " + stopRevision);
        }

        void QueueChange(PathChange change)
        {
            if (args.Filter != null && args.Filter.IsMatch(change.Path)) return;

            pendingReads.Increment();
            ThreadPool.QueueUserWorkItem(ProcessChange, change);
        }

        void ProcessChange(object data)
        {
            PathChange change = (PathChange) data;
            switch (change.Change)
            {
                case Change.Add: 
                    CreateDocument(change);
                    break;
                case Change.Replace:
                case Change.Modify:
                    FinalizeDocument(change);
                    CreateDocument(change);                   
                    break;
                case Change.Delete:
                    FinalizeDocument(change);
                    break;
            }
            pendingReads.Decrement();
        }

        void CreateDocument(PathChange change)
        {
            if (finalized.IsFinalized(change.Path, change.Revision)) return;

            PathData data = svn.GetPathData(change.Path, change.Revision);
            if (data == null) return;
            data.FirstRevision = change.Revision;
            data.LastRevision = headRevision;
            if (data.IsDirectory && change.IsCopy)
            {
                svn.ForEachChild(change.Path, change.Revision, QueueChange);
            }
            QueueIndexDocument(data);
        }

        void FinalizeDocument(PathChange change)
        {
            PathData data = svn.GetPathData(change.Path, change.Revision - 1);
            if (data == null) return;

            finalized.Finalize(change.Path, data.FirstRevision);
            QueueIndexDocument(data);            
        }

        void QueueIndexDocument(PathData data)
        {
            indexQueueLimit.WaitOne();
            lock (indexQueue)
            {
                indexQueue.Enqueue(data);
                indexQueueHasData.Set();
            }
        }

        void IndexThread()
        {
            WaitHandle[] wait = new WaitHandle[]{indexQueueHasData, pendingReads};
            for (; ; )
            {
                int waitResult = WaitHandle.WaitAny(wait);
                for (; ; )
                {
                    PathData data;
                    lock (indexQueue)
                    {
                        Console.WriteLine("QC: " + indexQueue.Count);
                        if (indexQueue.Count == 0)
                        {
                            indexQueueHasData.Reset();
                            break;                            
                        }
                        data = indexQueue.Dequeue();
                    }
                    indexQueueLimit.Release();
                    IndexDocument(data);
                }
                if (waitResult == 1) break;
            }
        }

        void IndexDocument(PathData data)
        {
            Console.WriteLine("{0}\t{1}\t{2}", data.FirstRevision, data.LastRevision, data.Path);
        }


        static bool IsValidPath(string path) // Path needs Processing
        {
            return !string.IsNullOrEmpty(path) && !path.ToLowerInvariant().Contains("/tags/");
        }

        void PrintProgress(char action, string path)
        {
            lock (this)
            {
                Console.Write(action);
                Console.Write((++indexedDocuments).ToString().PadLeft(8));
                Console.Write(obsolete_global_revision.ToString().PadLeft(8));
                Console.Write(" ");
                Console.WriteLine(path);
            }
        }

        void CreateDocument(string path)
        {
            if (!IsValidPath(path) || createdDocs.Contains(path)) return;
            createdDocs.Add(path);

            if (path[path.Length - 1] == '/' && !treeWalking)
            {
                treeWalking = true;
                svnlook.Run("tree " + repository + " \"" + path + "\" --full-paths -r" + obsolete_global_revision, CreateDocument);
                    // need "/" to prefix pathes            
                treeWalking = false;
            }
            PrintProgress('A', path);
            AddDocument(svninfo.ReadPathInfo(path, obsolete_global_revision), headRevision);
        }

        void UpdateDocument(string path)
        {
            if (!IsValidPath(path)) return;
            PrintProgress('U', path);
            SvnPathInfo info = svninfo.ReadPathInfo(path, obsolete_global_revision - 1);
            if (info != null) // atomic replace or copy and modify
            {
                indexWriter.DeleteDocuments(idTerm.CreateTerm(path));
                AddDocument(info, obsolete_global_revision - 1);
            }
            AddDocument(svninfo.ReadPathInfo(path, obsolete_global_revision), headRevision);
        }

        void DeleteDocument(string path)
        {
            if (!IsValidPath(path) || deletedDocs.Contains(path)) return;

            SvnPathInfo pathInfo = svninfo.ReadPathInfo(path, obsolete_global_revision - 1);
            if (pathInfo == null) return; // atomic replace

            if (path[path.Length - 1] == '/' && !treeWalking)
            {
                treeWalking = true;
                svnlook.Run("tree " + repository + " \"" + path + "\" --full-paths -r" + (obsolete_global_revision - 1), DeleteDocument);
                    // need "/" to prefix pathes            
                treeWalking = false;
            }

            PrintProgress('D', path);
            indexWriter.DeleteDocuments(idTerm.CreateTerm(path));
            deletedDocs.Add(path);
            AddDocument(pathInfo, obsolete_global_revision - 1);
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