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

        // reused objects for faster indexing
        readonly Term idTerm = new Term(FieldName.Id, "");
        readonly Field idField = new Field(FieldName.Id, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field revFirstField = new Field(FieldName.RevisionFirst, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field revLastField = new Field(FieldName.RevisionLast, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field sizeField = new Field(FieldName.Size, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field timestampField = new Field(FieldName.Timestamp, "", Field.Store.YES, Field.Index.NO);
        readonly Field authorField = new Field(FieldName.Author, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field typeField = new Field(FieldName.Type, "", Field.Store.NO, Field.Index.UN_TOKENIZED);
        readonly Field messageField;
        readonly Field pathField;
        readonly Field contentField;
        readonly Field externalsField;
        readonly PathTokenStream pathTokenStream = new PathTokenStream();
        readonly ContentTokenStream contentTokenStream = new ContentTokenStream();
        readonly ExternalsTokenStream externalsTokenStream = new ExternalsTokenStream();
        readonly ContentTokenStream messageTokenStream = new ContentTokenStream();

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
            ThreadPool.SetMaxThreads(args.MaxThreads / Environment.ProcessorCount + Environment.ProcessorCount, 1000);
            ThreadPool.SetMinThreads(args.MaxThreads, 100);

            svn = new SharpSvnApi(repository, args.User, args.Password);

            args.MaxRevision = Math.Min(args.MaxRevision, svn.GetYoungestRevision());

            contentField = new Field(FieldName.Content, contentTokenStream);
            pathField = new Field(FieldName.Path, pathTokenStream);
            externalsField = new Field(FieldName.Externals, externalsTokenStream);
            messageField = new Field(FieldName.Message, messageTokenStream);
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

            if (create || stopRevision % args.Optimize == 0 || stopRevision - startRevision > args.Optimize)
            {
                Console.WriteLine("Optimizing index ...");
                indexWriter.Optimize();
            }
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
            data.RevisionFirst = change.Revision;
            data.RevisionLast = headRevision;
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

            finalized.Finalize(change.Path, data.RevisionFirst);
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
            int printRev = data.RevisionLast == 99999999 ? args.MaxRevision : data.RevisionLast;
            Console.WriteLine("{0,8} {1,8} {2}", ++indexedDocuments, printRev, data.Path);

            Term id = idTerm.CreateTerm(data.Path + "@" + data.RevisionFirst);
            indexWriter.DeleteDocuments(id);
            Document doc = MakeDocument();

            idField.SetValue(id.Text());
            pathTokenStream.Reset(data.Path);
            revFirstField.SetValue(data.RevisionFirst.ToString("d8"));
            revLastField.SetValue(data.RevisionLast.ToString("d8"));
            authorField.SetValue(data.Author);
            timestampField.SetValue(data.Timestamp.ToString("yyyy-MM-dd hh:mm"));

            if (!data.IsDirectory)
            {
                sizeField.SetValue(PackedSizeConverter.ToSortableString(data.Size));
                doc.Add(sizeField);
            }

            if (contentTokenStream.SetText(data.Text))
                doc.Add(contentField);

            IndexProperties(doc, data.Properties);

            indexWriter.AddDocument(doc);            
        }

        void IndexProperties(Document doc, Dictionary<string, string> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.Key == "svn:externals")
                {
                    doc.Add(externalsField);
                    externalsTokenStream.SetText(prop.Value);
                }
                else if (prop.Key == "svn:mime-type")
                {
                    doc.Add(typeField);
                    typeField.SetValue(prop.Value);
                }
                else if (prop.Key == "svn:mergeinfo")
                {
                    continue; // do nothing
                }
                else
                {
                    doc.Add(new Field(prop.Key, prop.Value, Field.Store.NO, Field.Index.TOKENIZED));   
                }
            }
        }

        Document MakeDocument()
        {
            Document doc = new Document();
            doc.Add(idField);
            doc.Add(pathField);
            doc.Add(revFirstField);
            doc.Add(revLastField);
            doc.Add(timestampField);
            doc.Add(authorField);
            doc.Add(messageField);
            return doc;
        }
    }
}