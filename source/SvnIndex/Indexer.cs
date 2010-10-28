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
using SvnQuery.Lucene;
using SvnQuery.Svn;
using SvnQuery;

namespace SvnIndex
{
    /// <summary>
    /// Main class for creating and updating the repository index. 
    /// </summary>
    public class Indexer
    {
        const int MaxNumberOfTermsPerDocument = 50000;
        const int MaxDocumentSize =  1024 * 1024;

        readonly IndexerArgs _args;
        readonly Directory _indexDirectory;
        readonly ISvnApi _svn;

        readonly PendingJobs _pendingAnalyzeJobs = new PendingJobs();
        readonly PendingJobs _pendingFetchJobs = new PendingJobs();
        readonly Dictionary<string, IndexJobData> _headJobs = new Dictionary<string, IndexJobData>();
        readonly HighestRevision _highestRevision = new HighestRevision();

        readonly ManualResetEvent _stopIndexThread = new ManualResetEvent(false);
        readonly Semaphore _indexQueueLimit;
        readonly Queue<IndexJobData> _indexQueue = new Queue<IndexJobData>();
        readonly EventWaitHandle _indexQueueHasData = new ManualResetEvent(false);
        readonly EventWaitHandle _indexQueueIsEmpty = new ManualResetEvent(true);
        IndexWriter _indexWriter;

        // reused objects for faster indexing
        readonly Term _idTerm = new Term(FieldName.Id, "");
        readonly Field _idField = new Field(FieldName.Id, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field _revFirstField = new Field(FieldName.RevisionFirst, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field _revLastField = new Field(FieldName.RevisionLast, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field _sizeField = new Field(FieldName.Size, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field _timestampField = new Field(FieldName.Timestamp, "", Field.Store.YES, Field.Index.NO);
        readonly Field _authorField = new Field(FieldName.Author, "", Field.Store.YES, Field.Index.UN_TOKENIZED);
        readonly Field _typeField = new Field(FieldName.MimeType, "", Field.Store.NO, Field.Index.UN_TOKENIZED);
        readonly Field _messageField;
        readonly Field _pathField;
        readonly Field _contentField;
        readonly Field _externalsField;
        readonly SimpleTokenStream _pathTokenStream = new PathTokenStream();
        readonly SimpleTokenStream _contentTokenStream = new SimpleTokenStream();
        readonly SimpleTokenStream _externalsTokenStream = new PathTokenStream();
        readonly SimpleTokenStream _messageTokenStream = new SimpleTokenStream();

        public enum Command
        {
            Create,
            Update,
            Check
        } ;

        class AnalyzeJobData
        {
            public PathChange Change;
            public bool Recursive;
        }

        public class IndexJobData
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

            _args = args;
            _indexDirectory = FSDirectory.GetDirectory(args.IndexPath);
            _indexQueueLimit = new Semaphore(args.MaxThreads * 4, args.MaxThreads * 4);
            ThreadPool.SetMaxThreads(args.MaxThreads, 1000);
            ThreadPool.SetMinThreads(args.MaxThreads / 2, Environment.ProcessorCount);

            _contentField = new Field(FieldName.Content, _contentTokenStream);
            _pathField = new Field(FieldName.Path, _pathTokenStream);
            _externalsField = new Field(FieldName.Externals, _externalsTokenStream);
            _messageField = new Field(FieldName.Message, _messageTokenStream);

            _svn = new SharpSvnApi(args.RepositoryLocalUri, args.Credentials.User, args.Credentials.Password);
        }

        /// <summary>
        /// This constructor is intended for tests in RAMDirectory only
        /// </summary>
        public Indexer(IndexerArgs args, Directory dir) : this(args)
        {
            _indexDirectory = dir;
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
            int startRevision = 1;
            int stopRevision = Math.Min(_args.MaxRevision, _svn.GetYoungestRevision());
            bool optimize;

            Thread indexThread = new Thread(ProcessIndexQueue);
            indexThread.Name = "IndexThread";
            indexThread.IsBackground = true;
            indexThread.Start();
                                                    
            if (Command.Create == _args.Command)
            {
                _indexWriter = CreateIndexWriter(true);
                IndexProperty.SetSingleRevision(_indexWriter, _args.SingleRevision);
                QueueAnalyzeJob(new PathChange {Path = "/", Revision = 1, Change = Change.Add}); // add root directory manually
                optimize = true;
            }
            else // Command.Update
            {
                IndexReader reader = IndexReader.Open(_indexDirectory); // important: create reader before creating indexWriter!
                _highestRevision.Reader = reader;
                startRevision = IndexProperty.GetRevision(reader) + 1;
                _args.SingleRevision = IndexProperty.GetSingleRevision(reader);
                if (_args.SingleRevision) Console.WriteLine("SingleRevision index");
                _indexWriter = CreateIndexWriter(false);
                optimize = stopRevision % _args.Optimize == 0 || stopRevision - startRevision > _args.Optimize;
            }
            IndexProperty.SetRepositoryLocalUri(_indexWriter, _args.RepositoryLocalUri);
            IndexProperty.SetRepositoryExternalUri(_indexWriter, _args.RepositoryExternalUri);
            IndexProperty.SetRepositoryName(_indexWriter, _args.RepositoryName);
            IndexProperty.SetRepositoryCredentials(_indexWriter, _args.Credentials);

            while (startRevision <= stopRevision)
            {
                IndexRevisionRange(startRevision, Math.Min(startRevision + _args.CommitInterval - 1, stopRevision));
                startRevision += _args.CommitInterval;

                if (startRevision <= stopRevision)
                {
                    if (_highestRevision.Reader != null) _highestRevision.Reader.Close();
                    CommitIndex();
                    _highestRevision.Reader = IndexReader.Open(_indexDirectory);
                    _indexWriter = CreateIndexWriter(false);
                }
            }
            _stopIndexThread.Set();
            if (_highestRevision.Reader != null) _highestRevision.Reader.Close();
            _highestRevision.Reader = null;
            if (optimize)
            {
                Console.WriteLine("Optimizing index ...");
                _indexWriter.Optimize();
            }
            CommitIndex();
            TimeSpan time = DateTime.UtcNow - start;
            Console.WriteLine("Finished in {0:00}:{1:00}:{2:00}", time.Hours, time.Minutes, time.Seconds);
        }

        IndexWriter CreateIndexWriter(bool pCreateNewIndex)
        {
            var indexWriter = new IndexWriter(_indexDirectory, false, null, pCreateNewIndex);
            indexWriter.SetMaxFieldLength(MaxNumberOfTermsPerDocument);
            return indexWriter;
        }

        void CommitIndex()
        {
            Console.WriteLine("Commit index");
            _indexWriter.Close();
        }

        void IndexRevisionRange(int startRevision, int stopRevision)
        {
            foreach (var data in _svn.GetRevisionData(startRevision, stopRevision))
            {
                IndexJobData jobData = new IndexJobData();
                if (!_args.SingleRevision)
                {
                    jobData.Path = "$Revision " + data.Revision;
                    jobData.RevisionFirst = data.Revision;
                    jobData.RevisionLast = data.Revision;
                    jobData.Info = new PathInfo();
                    jobData.Info.Author = data.Author;
                    jobData.Info.Timestamp = data.Timestamp;
                    QueueIndexJob(jobData);
                }
                data.Changes.ForEach(QueueAnalyzeJobRecursive);
                _pendingAnalyzeJobs.Wait();
            }

            foreach (var job in _headJobs.Values) // no lock necessary because no analyzeJobs are running
            {
                QueueFetchJob(job);
            }
            _headJobs.Clear();

            _pendingFetchJobs.Wait();
            _indexQueueIsEmpty.WaitOne();

            IndexProperty.SetRevision(_indexWriter, stopRevision);
            Console.WriteLine("Index revision is now " + stopRevision);
        }

        void QueueAnalyzeJobRecursive(PathChange change)
        {
            if (IgnorePath(change.Path)) return;
            QueueAnalyzeJob(new AnalyzeJobData {Change = change, Recursive = true});
        }

        void QueueAnalyzeJob(PathChange change)
        {
            if (IgnorePath(change.Path)) return;
            QueueAnalyzeJob(new AnalyzeJobData {Change = change, Recursive = false});
        }

        bool IgnorePath(string path)
        {
            return _args.Filter.IsMatch(path);
        }

        void QueueAnalyzeJob(AnalyzeJobData jobData)
        {
            _pendingAnalyzeJobs.Increment();
            ThreadPool.QueueUserWorkItem(AnalyzeJob, jobData);
        }

        // The ThreadPool entry point for an AnalyzeJob.
        // ThreadPool Exceptions are catched by the AppDomain Unhandled Exception Handler
        void AnalyzeJob(object data)
        {
            AnalyzeJob((AnalyzeJobData) data);
            _pendingAnalyzeJobs.Decrement();
        }

        void AnalyzeJob(AnalyzeJobData jobData)
        {
            string path = jobData.Change.Path;
            int revision = jobData.Change.Revision;

            if (_args.Verbosity > 3)
                Console.WriteLine("Analyze " + jobData.Change.Change.ToString().PadRight(7) + path + "   " + revision);
            switch (jobData.Change.Change)
            {
                case Change.Add:
                    AddPath(path, revision, jobData.Recursive && jobData.Change.IsCopy);
                    break;
                case Change.Replace:
                    DeletePath(path, revision, jobData.Recursive);
                    AddPath(path, revision, jobData.Recursive && jobData.Change.IsCopy);
                    break;
                case Change.Modify:
                    DeletePath(path, revision, false);
                    AddPath(path, revision, false);
                    break;
                case Change.Delete:
                    DeletePath(path, revision, jobData.Recursive);
                    break;
            }
        }

        void AddPath(string path, int revision, bool recursive)
        {
            if (!_highestRevision.Set(path, revision)) return;

            IndexJobData jobData = new IndexJobData();
            jobData.Path = path;
            jobData.RevisionFirst = revision;
            jobData.RevisionLast = Revision.Head;
            jobData.Info = _svn.GetPathInfo(path, revision);
            if (jobData.Info == null) return; // workaround for issues with forbidden characters in local repository access
            lock (_headJobs) _headJobs[path] = jobData;

            if (recursive && jobData.Info.IsDirectory)
            {
                _svn.ForEachChild(path, revision, Change.Add, QueueAnalyzeJob);
            }
        }

        void DeletePath(string path, int revision, bool recursive)
        {
            IndexJobData jobData;
            lock (_headJobs) _headJobs.TryGetValue(path, out jobData);
            if (jobData != null)
            {
                lock (_headJobs) _headJobs.Remove(path);
            }
            else
            {
                int highest = _highestRevision.Get(path);
                if (highest == 0) return; // an atomic delete inside a svn copy operation

                jobData = new IndexJobData();
                jobData.Path = path;
                jobData.RevisionFirst = highest;
                jobData.Info = _svn.GetPathInfo(path, highest);
            }
            jobData.RevisionLast = revision - 1;
            _highestRevision.Set(path, 0);

            if (jobData.Info == null) return; // workaround for issues with forbidden characters in local repository access
            if (recursive && jobData.Info.IsDirectory)
            {
                _svn.ForEachChild(path, revision, Change.Delete, QueueAnalyzeJob);
            }
            QueueFetchJob(jobData);
        }

        void QueueFetchJob(IndexJobData jobData)
        {
            _pendingFetchJobs.Increment();
            ThreadPool.QueueUserWorkItem(FetchJob, jobData);
        }

        // The ThreadPool entry point for an AnalyzeJob.
        // ThreadPool Exceptions are catched by the AppDomain Unhandled Exception Handler
        void FetchJob(object data)
        {
            FetchJob((IndexJobData) data);
            _pendingFetchJobs.Decrement();
        }

        /// <summary>
        /// Fetches some more information from the repository for an item
        /// </summary>
        /// <param name="jobData"></param>
        void FetchJob(IndexJobData jobData)
        {
            if (!_args.SingleRevision || jobData.RevisionLast == Revision.Head) // don't fetch if this data would be deleted anyway
            {
                if (_args.Verbosity > 1)
                    Console.WriteLine("Fetch          " + jobData.Path + "   " + jobData.RevisionFirst + ":" + jobData.RevisionLast);

                jobData.Properties = _svn.GetPathProperties(jobData.Path, jobData.RevisionFirst);
                string mime;
                bool isText = !jobData.Properties.TryGetValue("svn:mime-type", out mime) || mime.StartsWith("text/");
               
                if (!jobData.Info.IsDirectory && isText && 0 < jobData.Info.Size && jobData.Info.Size < MaxDocumentSize)
                {
                    jobData.Content = _svn.GetPathContent(jobData.Path, jobData.RevisionFirst, jobData.Info.Size);
                }
            }
            QueueIndexJob(jobData);
        }

        void QueueIndexJob(IndexJobData jobData)
        {
            _indexQueueLimit.WaitOne();
            lock (_indexQueue)
            {
                _indexQueue.Enqueue(jobData);
                _indexQueueHasData.Set();
                _indexQueueIsEmpty.Reset();
            }
        }

        /// <summary>
        /// processes the index queue until there are no more pending reads and the queue is empty
        /// </summary>
        void ProcessIndexQueue()
        {
            WaitHandle[] wait = new WaitHandle[] {_indexQueueHasData, _stopIndexThread};
            while (wait[WaitHandle.WaitAny(wait)] != _stopIndexThread)
            {
                for (;;)
                {
                    IndexJobData data;
                    lock (_indexQueue)
                    {
                        if (_indexQueue.Count == 0)
                        {
                            _indexQueueHasData.Reset();
                            _indexQueueIsEmpty.Set();
                            break;
                        }
                        data = _indexQueue.Dequeue();
                    }
                    _indexQueueLimit.Release();
                    IndexDocument(data);
                }
            }
        }

        void IndexDocument(IndexJobData data)
        {
            if (_args.Verbosity == 0 && data.Path[0] == '$')
                Console.WriteLine("Revision " + data.RevisionFirst);
            else
                Console.WriteLine("Index {0}   {1}:{2}", data.Path, data.RevisionFirst, data.RevisionLast);

            string idText = data.Path[0] == '$' ? data.Path : data.Path + "@" + data.RevisionFirst;
            Term id = _idTerm.CreateTerm(idText);
            _indexWriter.DeleteDocuments(id);

            if (_args.SingleRevision && data.RevisionLast != Revision.Head)
                return;

            Document doc = MakeDocument();
            _idField.SetValue(idText);
            _pathTokenStream.Text = data.Path;
            _revFirstField.SetValue(data.RevisionFirst.ToString(RevisionFilter.RevFormat));
            _revLastField.SetValue(data.RevisionLast.ToString(RevisionFilter.RevFormat));
            _authorField.SetValue(data.Info.Author.ToLowerInvariant());
            SetTimestampField(data.Info.Timestamp);
            _messageTokenStream.Text = _svn.GetLogMessage(data.RevisionFirst);

            if (!data.Info.IsDirectory)
            {
                _sizeField.SetValue(PackedSizeConverter.ToSortableString(data.Info.Size));
                doc.Add(_sizeField);
            }

            _contentTokenStream.Text = data.Content;
            if (!_contentTokenStream.IsEmpty) doc.Add(_contentField);

            IndexProperties(doc, data.Properties);

            _indexWriter.AddDocument(doc);
        }

        void SetTimestampField(DateTime dt)
        {
            _timestampField.SetValue(dt.ToString("yyyy-MM-dd hh:mm"));
        }

        void IndexProperties(Document doc, IDictionary<string, string> properties)
        {
            if (properties == null) return;
            foreach (var prop in properties)
            {
                if (prop.Key == "svn:externals")
                {
                    doc.Add(_externalsField);
                    _externalsTokenStream.Text = prop.Value;
                }
                else if (prop.Key == "svn:mime-type")
                {
                    doc.Add(_typeField);
                    _typeField.SetValue(prop.Value);
                }
                else if (prop.Key == "svn:mergeinfo")
                {
                    continue; // don't index
                }
                else
                {
                    doc.Add(new Field(prop.Key, new SimpleTokenStream {Text = prop.Value}));
                }
            }
        }

        Document MakeDocument()
        {
            var doc = new Document();
            doc.Add(_idField);
            doc.Add(_revFirstField);
            doc.Add(_revLastField);
            doc.Add(_timestampField);
            doc.Add(_authorField);
            doc.Add(_messageField);
            doc.Add(_pathField);
            return doc;
        }
    }
}