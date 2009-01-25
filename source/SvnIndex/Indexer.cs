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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace SvnQuery
{
    /// <summary>
    /// Main class for creating and updating the repository index. 
    /// </summary>
    public class Indexer
    {
        readonly IndexerArgs args;
        readonly Directory indexDirectory;
        readonly ISvnApi svn;
        
        readonly PendingJobs pendingAnalyzeJobs = new PendingJobs();
        readonly PendingJobs pendingFetchJobs = new PendingJobs();
        readonly Dictionary<string, IndexJob> headJobs = new Dictionary<string, IndexJob>();
        readonly HighestRevision highestRevision = new HighestRevision();

        readonly ManualResetEvent stopIndexThread = new ManualResetEvent(false);
        readonly Semaphore indexQueueLimit;
        readonly Queue<IndexJob> indexQueue = new Queue<IndexJob>();
        readonly EventWaitHandle indexQueueHasData = new ManualResetEvent(false);
        readonly EventWaitHandle indexQueueIsEmpty = new ManualResetEvent(true);
        int indexedDocuments;
        IndexWriter indexWriter;

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
            Update, 
            Check
        } ;

        class AnalyzeJob
        {
            public PathChange Change;
            public bool Recursive;
        }

        public class IndexJob
        {
            public string Path;
            public int RevisionFirst;
            public int RevisionLast;
            public PathInfo Info;
            public string Content;
            public IDictionary<string, string> Properties;        
        }

        public Indexer(IndexerArgs args)
        {
            WriteLogo();
            WriteArgs();

            this.args = args;
            indexDirectory = FSDirectory.GetDirectory(args.IndexPath);
            indexQueueLimit = new Semaphore(args.MaxThreads * 4, args.MaxThreads * 4);
            ThreadPool.SetMaxThreads(args.MaxThreads, 1000);
            ThreadPool.SetMinThreads(args.MaxThreads / 2, Environment.ProcessorCount);

            contentField = new Field(FieldName.Content, contentTokenStream);
            pathField = new Field(FieldName.Path, pathTokenStream);
            externalsField = new Field(FieldName.Externals, externalsTokenStream);
            messageField = new Field(FieldName.Message, messageTokenStream);

            svn = new SharpSvnApi(args.RepositoryUri, args.User, args.Password);

        }

        /// <summary>
        /// This constructor is intended for tests in RAMDirectory only
        /// </summary>
        public Indexer(IndexerArgs args, Directory dir): this(args)
        {
            indexDirectory = dir;
        }

        static void WriteLogo()
        {
            Console.WriteLine();
            AssemblyName name = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine(name.Name + " " + name.Version);
        }

        static void WriteArgs()
        {
            Console.WriteLine();
        }

        public void Run()
        {
            Console.WriteLine("Begin indexing ...");
            DateTime start = DateTime.UtcNow;
            bool create = args.Command == Command.Create;
            int startRevision = 1; 
            int stopRevision = Math.Min(args.MaxRevision, svn.GetYoungestRevision());
            bool optimize = create || stopRevision % args.Optimize == 0 || stopRevision - startRevision > args.Optimize;

            Thread indexThread = new Thread(ProcessIndexQueue);
            indexThread.Name = "IndexThread";
            indexThread.IsBackground = true;
            indexThread.Start();

            indexedDocuments = 0;
            if (!create)
            {
                IndexReader reader = IndexReader.Open(indexDirectory);
                highestRevision.Reader = reader;
                startRevision = IndexProperty.GetRevision(reader) + 1;
                Guid repositoryId = IndexProperty.GetRepositoryId(reader);
                indexedDocuments = IndexProperty.GetDocumentCount(reader);
                                
                if (svn.GetRepositoryId() != repositoryId)
                    Console.WriteLine("WARNING: Existing index was created from a different repository. (UUID does not match)");
            }
           
            indexWriter = new IndexWriter(indexDirectory, false, null, create);
            if (create) IndexProperty.SetRepositoryId(indexWriter, svn.GetRepositoryId());
            if (args.RepositoryName != null) IndexProperty.SetRepositoryName(indexWriter, args.RepositoryName);
            IndexProperty.SetRepositoryUri(indexWriter, args.RepositoryUri);

            if (create) QueueAnalyze(new PathChange { Path = "/", Revision = 1, Change = Change.Add }); // add root directory manually
            while (startRevision <= stopRevision) 
            {
                IndexRevisionRange(startRevision, Math.Min(startRevision + args.CommitInterval - 1, stopRevision));
                if (highestRevision.Reader != null) highestRevision.Reader.Close();
                startRevision += args.CommitInterval;                     
                if (startRevision > stopRevision) break;
                CommitIndex();
                highestRevision.Reader = IndexReader.Open(indexDirectory);
                indexWriter = new IndexWriter(indexDirectory, false, null, false);
            }
            stopIndexThread.Set();
            if (optimize)
            {
                Console.WriteLine("Optimizing index ...");
                indexWriter.Optimize();
            }
            CommitIndex();
            TimeSpan time = DateTime.UtcNow - start;            
            Console.WriteLine("Finished in {0:00}:{1:00}:{2:00}", time.Hours, time.Minutes, time.Seconds);
        }

        void CommitIndex()
        {
            Console.WriteLine("Commit index");
            indexWriter.Close();
        }

        void IndexRevisionRange(int start, int stop)
        {
            foreach (var data in svn.GetRevisionData(start, stop))
            {
                IndexJob job = new IndexJob();
                job.Path = "$Revision";
                job.RevisionFirst = data.Revision;
                job.RevisionLast = data.Revision;
                job.Info = new PathInfo();
                job.Info.Author = data.Author;
                job.Info.Timestamp = data.Timestamp;
                QueueIndexJob(job);
                data.Changes.ForEach(QueueAnalyzeRecursive);
                pendingAnalyzeJobs.Wait();
            }

            foreach (var job in headJobs.Values) // no lock necessary because no analyzeJobs are running
            {
                QueueFetch(job);
            }
            headJobs.Clear();

            pendingFetchJobs.Wait();
            indexQueueIsEmpty.WaitOne();

            IndexProperty.SetRevision(indexWriter, stop);
            IndexProperty.SetDocumentCount(indexWriter, indexedDocuments);
            Console.WriteLine("Index revision is now " + stop);
        }

        void QueueAnalyzeRecursive(PathChange change)
        {
            if (IgnorePath(change.Path)) return;
            QueueAnalyze(new AnalyzeJob { Change = change, Recursive = true });
        }

        void QueueAnalyze(PathChange change)
        {
            if (IgnorePath(change.Path)) return;
            QueueAnalyze(new AnalyzeJob { Change = change, Recursive = false });
        }

        bool IgnorePath(string path)
        {
            return args.Filter != null && args.Filter.IsMatch(path);
        }

        void QueueAnalyze(AnalyzeJob job)
        {
            pendingAnalyzeJobs.Increment();
            ThreadPool.QueueUserWorkItem(Analyze, job);
        }

        void Analyze(object data)
        {
            Analyze((AnalyzeJob) data);
            pendingAnalyzeJobs.Decrement();

#warning "Need a better way to communicate errors";

//            catch (Exception x)
//            {
//                Console.WriteLine("Exception in ThreadPool Thread: " + x);
//                Environment.Exit(-100);
//            }
        }

        void Analyze(AnalyzeJob job)
        {
            string path = job.Change.Path;
            int revision = job.Change.Revision;
         
            if (args.Verbosity > 3)
                Console.WriteLine("Analyze " + job.Change.Change.ToString().PadRight(7) + path + "   " + revision);
            switch (job.Change.Change)
            {
                case Change.Add:
                    AddPath(path, revision, job.Recursive && job.Change.IsCopy);
                    break;
                case Change.Replace:
                    DeletePath(path, revision, job.Recursive);
                    AddPath(path, revision, job.Recursive && job.Change.IsCopy);
                    break;                    
                case Change.Modify:
                    DeletePath(path, revision, false);
                    AddPath(path, revision, false);
                    break;
                case Change.Delete:
                    DeletePath(path, revision, job.Recursive);
                    break;
            }
        }

        void AddPath(string path, int revision, bool recursive)
        {            
            if (!highestRevision.Set(path, revision)) return;

            IndexJob job = new IndexJob();
            job.Path = path;
            job.RevisionFirst = revision;
            job.RevisionLast = RevisionFilter.Head;
            job.Info = svn.GetPathInfo(path, revision);
            lock (headJobs) headJobs[path] = job;

            if (recursive && job.Info.IsDirectory)
            {
                svn.ForEachChild(path, revision, Change.Add, QueueAnalyze);
            }            
        }

        void DeletePath(string path, int revision, bool recursive)
        {
            IndexJob job;
            lock (headJobs) headJobs.TryGetValue(path, out job);
            if (job != null)
            {
                lock (headJobs) headJobs.Remove(path);
            }
            else
            {
                int highest = highestRevision.Get(path);
                if (highest == 0) return; // an atomic delete inside a copy operation

                job = new IndexJob();
                job.Path = path;
                job.RevisionFirst = highest;
                job.Info = svn.GetPathInfo(path, highest);
            }
            job.RevisionLast = revision - 1;
            if (recursive && job.Info.IsDirectory)
            {
                svn.ForEachChild(path, revision, Change.Delete, QueueAnalyze);
            }
            QueueFetch(job);
        }

        void QueueFetch(IndexJob job)
        {
            pendingFetchJobs.Increment();
            ThreadPool.QueueUserWorkItem(Fetch, job);
        }

        void Fetch(object data)
        {
            Fetch((IndexJob) data);
            pendingFetchJobs.Decrement();

#warning "Need a better way to communicate thread pool errors";
        }

        void Fetch(IndexJob job)
        {            
            if (args.Verbosity > 1)
                Console.WriteLine("Fetch          " + job.Path + "   " + job.RevisionFirst + ":" + job.RevisionLast);
          
            job.Properties = svn.GetPathProperties(job.Path, job.RevisionFirst);
            //if (job.Info.IsDirectory && !job.Path.EndsWith("/"))
            //{
            //    job.Path += "/";
            //}
            string mime;
            bool isText = !job.Properties.TryGetValue("svn:mime-type", out mime) || mime.StartsWith("text/");          
            const int MaxFileSize = 128*1024*1024;
            if (!job.Info.IsDirectory && isText && 0 < job.Info.Size && job.Info.Size < MaxFileSize)
            {
                job.Content = svn.GetPathContent(job.Path, job.RevisionFirst, job.Info.Size);
            }
            QueueIndexJob(job);
        }

        void QueueIndexJob(IndexJob job)
        {
            indexQueueLimit.WaitOne();
            lock (indexQueue)
            {
                indexQueue.Enqueue(job);
                indexQueueHasData.Set();
                indexQueueIsEmpty.Reset();
            }
        }

        /// <summary>
        /// processes the index queue until there are no more pending reads and the queue is empty
        /// </summary>
        void ProcessIndexQueue()       
        {
            WaitHandle[] wait = new WaitHandle[] {indexQueueHasData, stopIndexThread};
            while (wait[WaitHandle.WaitAny(wait)] != stopIndexThread)
            {                
                for (;;)
                {
                    IndexJob data;
                    lock (indexQueue)
                    {
                        if (indexQueue.Count == 0)
                        {
                            indexQueueHasData.Reset();
                            indexQueueIsEmpty.Set();
                            break;
                        }
                        data = indexQueue.Dequeue();
                    }
                    indexQueueLimit.Release();
                    IndexDocument(data);
                }
            }
        }

        void IndexDocument(IndexJob data)
        {
            ++indexedDocuments;
            if (args.Verbosity == 0 && data.Path[0] == '$')
                Console.WriteLine("Revision " + data.RevisionFirst);
            else 
                Console.WriteLine("Index {0,8} {1}   {2}:{3}", indexedDocuments, data.Path, data.RevisionFirst, data.RevisionLast);

            Term id = idTerm.CreateTerm(data.Path + "@" + data.RevisionFirst);
            indexWriter.DeleteDocuments(id);
            Document doc = MakeDocument();

            idField.SetValue(id.Text());
            pathTokenStream.Reset(data.Path);
            revFirstField.SetValue(data.RevisionFirst.ToString("d8"));
            revLastField.SetValue(data.RevisionLast.ToString("d8"));
            authorField.SetValue(data.Info.Author.ToLowerInvariant());
            SetTimestampField(data.Info.Timestamp);
            messageTokenStream.SetText(svn.GetLogMessage(data.RevisionFirst));

            if (!data.Info.IsDirectory)
            {
                sizeField.SetValue(PackedSizeConverter.ToSortableString(data.Info.Size));
                doc.Add(sizeField);
            }

            if (contentTokenStream.SetText(data.Content))
                doc.Add(contentField);

            IndexProperties(doc, data.Properties);

            indexWriter.AddDocument(doc);
        }

        void SetTimestampField(DateTime dt)
        {
            timestampField.SetValue(dt.ToString("yyyy-MM-dd hh:mm"));
        }

        void IndexProperties(Document doc, IDictionary<string, string> properties)
        {
            if (properties == null) return;
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

        Document MakeDocument()
        {
            var doc = new Document();
            doc.Add(idField);
            doc.Add(revFirstField);
            doc.Add(revLastField);
            doc.Add(timestampField);
            doc.Add(authorField);
            doc.Add(messageField);
            doc.Add(pathField);
            return doc;
        }

    }
}