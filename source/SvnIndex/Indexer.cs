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
using System.Reflection;
using System.Threading;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
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
        readonly ISvnApi svn;

        readonly FinalizedMemory finalized = new FinalizedMemory();
        readonly PendingReads pendingReads = new PendingReads();

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
        readonly Field typeField = new Field(FieldName.MimeType, "", Field.Store.NO, Field.Index.UN_TOKENIZED);
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
            PrintLogo();

            this.args = args;
            indexQueueLimit = new Semaphore(args.MaxThreads * 4, args.MaxThreads * 4);
            ThreadPool.SetMaxThreads(args.MaxThreads, 1000);
            ThreadPool.SetMinThreads(args.MaxThreads / 2, Environment.ProcessorCount);

            contentField = new Field(FieldName.Content, contentTokenStream);
            pathField = new Field(FieldName.Path, pathTokenStream);
            externalsField = new Field(FieldName.Externals, externalsTokenStream);
            messageField = new Field(FieldName.Message, messageTokenStream);

            svn = new SharpSvnApi(args.RepositoryUri, args.User, args.Password);

            PrintArgs();
        }

        static void PrintLogo()
        {
            Console.WriteLine();
            AssemblyName name = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine(name.Name + " " + name.Version);
        }

        static void PrintArgs()
        {
            Console.WriteLine();
        }

        public void Run()
        {
            Console.WriteLine("Validating parameters ...");
            DateTime start = DateTime.UtcNow;
            bool create = args.Command == Command.Create;
            int startRevision = 1; 
            int stopRevision = Math.Min(args.MaxRevision, svn.GetYoungestRevision());
            bool optimize = create || stopRevision % args.Optimize == 0 || stopRevision - startRevision > args.Optimize;
            if (!create)
            {
                IndexSearcher searcher = new IndexSearcher(args.IndexPath);
                startRevision = IndexProperty.GetRevision(searcher.Reader) + 1;
                Guid repositoryId = IndexProperty.GetRepositoryId(searcher.Reader); 
                searcher.Close();
                if (svn.GetRepositoryId() != repositoryId)
                    throw new Exception("Index does not belong to repository. Index repository uuid = " + repositoryId);
            }

            if (startRevision > stopRevision)
            {
                Console.WriteLine("Nothing to do. Index revision is " + (startRevision - 1));
            }
            else
            {
                Console.WriteLine("Begin indexing ...");
                var dummy = new StandardAnalyzer();
                for (int i = startRevision; i <= stopRevision; i += args.CommitInterval)
                {
                    indexWriter = new IndexWriter(FSDirectory.GetDirectory(args.IndexPath), false, dummy, create);
                    if (create) IndexProperty.UpdateRepositoryId(indexWriter, svn.GetRepositoryId());
                    create = false;
                    indexWriter.SetRAMBufferSizeMB(32);
                    IndexRevisionRange(startRevision, Math.Min(startRevision + args.CommitInterval - 1, stopRevision));
                    startRevision += args.CommitInterval;
                    if (startRevision > stopRevision && optimize)
                    { 
                        Console.WriteLine("Optimizing index ...");
                        indexWriter.Optimize();
                    }
                    indexWriter.Close(); // Commit changes
                }
                indexWriter = null;
            }
            Console.WriteLine("Finished in " + (DateTime.UtcNow - start));
        }

        void IndexRevisionRange(int start, int stop)
        {
            var doc = MakeRevisionDocument();

            // reverse order to minimize document updates 
            foreach (var data in svn.GetRevisionData(stop, start))
            {
                data.Changes.ForEach(QueueChange);
                idField.SetValue("$Revision " + data.Revision);
                revFirstField.SetValue(data.Revision.ToString("d8"));
                revLastField.SetValue(data.Revision.ToString("d8"));
                authorField.SetValue(data.Author.ToLowerInvariant());
                SetTimestampField(data.Timestamp);
                messageTokenStream.SetText(data.Message);
                indexWriter.AddDocument(doc);
            }

            ProcessIndexQueue(); 

            IndexProperty.UpdateRevision(indexWriter, stop);
            Console.WriteLine("Index revision is now " + stop);
        }

        void QueueChange(PathChange change)
        {
            if (args.Filter != null && args.Filter.IsMatch(change.Path)) return;

            pendingReads.Increment();
            ThreadPool.QueueUserWorkItem(ProcessChange, change);
        }

        void ProcessChange(object data)
        {
            try
            {
                var change = (PathChange) data;
                Console.WriteLine(change);
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
            catch (Exception x)
            {
                Console.WriteLine("Exception in ThreadPool Thread: " + x);
                Environment.Exit(-100);
            }
        }

        void CreateDocument(PathChange change)
        {
            if (finalized.IsFinalized(change.Path, change.Revision)) return;

            PathData data = svn.GetPathData(change.Path, change.Revision);
            if (data == null) return;
            data.Revision = change.Revision;
            data.FinalRevision = headRevision;
            if (data.IsDirectory && change.IsCopy)
            {
                svn.AddDirectoryChildren(change.Path, change.Revision, QueueChange);
            }
            QueueIndexDocument(data);
        }

        void FinalizeDocument(PathChange change)
        {
            PathData data = svn.GetPathData(change.Path, change.Revision - 1);
            if (data == null) return;

            finalized.Finalize(change.Path, data.Revision);
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

        /// <summary>
        /// processes the index queue until there are no more pending reads and the queue is empty
        /// </summary>
        void ProcessIndexQueue()
        {
            WaitHandle[] wait = new WaitHandle[] {indexQueueHasData, pendingReads};
            for (;;)
            {
                int waitResult = WaitHandle.WaitAny(wait);
                for (;;)
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
            char flag = data.FinalRevision == headRevision ? 'H' : 'F';
            Console.WriteLine("{0,8} {4} {1} => {2}:{3}", ++indexedDocuments, data.Path, data.Revision, data.FinalRevision, flag);

            Term id = idTerm.CreateTerm(data.Path + "@" + data.Revision);
            indexWriter.DeleteDocuments(id);
            Document doc = MakePathDocument();

            idField.SetValue(id.Text());
            pathTokenStream.Reset(data.Path);
            revFirstField.SetValue(data.Revision.ToString("d8"));
            revLastField.SetValue(data.FinalRevision.ToString("d8"));
            authorField.SetValue(data.Author.ToLowerInvariant());
            SetTimestampField(data.Timestamp);
            messageTokenStream.SetText(svn.GetLogMessage(data.Revision));

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

        void SetTimestampField(DateTime dt)
        {
            timestampField.SetValue(dt.ToString("yyyy-MM-dd hh:mm"));
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
                    continue; // don't index
                }
                else
                {
                    doc.Add(new Field(prop.Key, new ContentTokenStream(prop.Value, false)));
                }
            }
        }

        Document MakeBaseDocument()
        {
            var doc = new Document();
            doc.Add(idField);
            doc.Add(revFirstField);
            doc.Add(revLastField);
            doc.Add(timestampField);
            doc.Add(authorField);
            doc.Add(messageField);
            return doc;
        }

        Document MakePathDocument()
        {
            var doc = MakeBaseDocument();
            doc.Add(pathField);
            return doc;
        }

        Document MakeRevisionDocument()
        {
            var doc = MakeBaseDocument();
            doc.Add(new Field(FieldName.IsRevision, "true", Field.Store.NO, Field.Index.UN_TOKENIZED));
            return doc;            
        }
    }
}