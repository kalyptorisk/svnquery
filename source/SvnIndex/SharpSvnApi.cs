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
using System.Collections.ObjectModel;
using System.IO;
using SharpSvn;

namespace SvnQuery
{
    public class SharpSvnApi : ISvnApi
    {
        readonly Uri uri;
        readonly string user;
        readonly string password;
        readonly Dictionary<int, string> messages = new Dictionary<int, string>();
        readonly List<SvnClient> clientPool = new List<SvnClient>();
        

        public SharpSvnApi(string repositoryUrl) : this(repositoryUrl, "", "")
        {}

        public SharpSvnApi(string repositoryUri, string user, string password)
        {
            uri = new Uri(repositoryUri);
            this.user = user;
            this.password = password;
        }

        SvnClient AllocSvnClient()
        {
            SvnClient client = null;
            lock (clientPool)
            {
                int last = clientPool.Count - 1;
                if (last >= 0)
                {
                    client = clientPool[last];
                    clientPool.RemoveAt(last);
                }
            }

            if (client == null) client = new SvnClient();
            client.Authentication.UserNameHandlers += (s, e) => e.UserName = user;
            client.Authentication.UserNamePasswordHandlers += (s, e) =>
            {
                e.UserName = user;
                e.Password = password;
            };
            return client;
        }

        void FreeSvnClient(SvnClient client)
        {
            lock (clientPool) clientPool.Add(client);
        }

        SvnInfoEventArgs Info
        {
            get
            {
                if (_info == null)
                {
                    SvnClient client = AllocSvnClient();
                    SvnTarget target = new SvnUriTarget(uri);
                    client.GetInfo(target, out _info);
                    FreeSvnClient(client);
                }
                return _info;
            }
        }
        SvnInfoEventArgs _info;

        public int GetYoungestRevision()
        {
            return (int)Info.LastChangeRevision;
        }

        public Guid GetRepositoryId()
        {
            return Info.RepositoryId;                  
        }

        public string GetLogMessage(int revision)
        {
            string message;
            lock (messages) messages.TryGetValue(revision, out message);
            if (message == null)
            {
                SvnClient client = AllocSvnClient();
                try
                {
                    SvnUriTarget target = new SvnUriTarget(uri);
                    client.GetRevisionProperty(target, "svn:log", out message); 
                    message = "";
                }
                finally
                {
                    FreeSvnClient(client);
                }
                lock (messages) messages[revision] = message;
            }
            return message;
        }

        public List<RevisionData> GetRevisionData(int firstRevision, int lastRevision)
        {
            List<RevisionData> revisions = new List<RevisionData>();
            SvnClient client = AllocSvnClient();
            try
            {
                SvnLogArgs args = new SvnLogArgs(new SvnRevisionRange(firstRevision, lastRevision));
                args.StrictNodeHistory = false;
                args.RetrieveChangedPaths = true;
                args.ThrowOnError = true;
                args.ThrowOnCancel = true;
                Collection<SvnLogEventArgs> logEvents;
                client.GetLog(uri, args, out logEvents);
                foreach (SvnLogEventArgs e in logEvents)
                {
                    RevisionData data = new RevisionData();
                    data.Revision = (int) e.Revision;
                    data.Author = e.Author ?? "";
                    data.Message = e.LogMessage ?? "";
                    data.Timestamp = e.Time;
                    AddChanges(data, e.ChangedPaths);                    
                    revisions.Add(data);
                                
                    lock (messages) messages[data.Revision] = data.Message;
                }
            }
            finally
            {
                FreeSvnClient(client);
            }
            return revisions;
        }

        static void AddChanges(RevisionData data, IEnumerable<SvnChangeItem> changes)
        {
            if (changes == null) return;
            foreach (var item in changes)
            {
                data.Changes.Add(new PathChange
                                 {
                                     Change = ConvertActionToChange(item.Action),
                                     Revision = data.Revision,
                                     Path = item.Path,
                                     IsCopy = item.CopyFromPath != null
                                 });
            }
        }

        static Change ConvertActionToChange(SvnChangeAction action)
        {
            switch (action)
            {
                case SvnChangeAction.Add: return Change.Add;
                case SvnChangeAction.Modify: return Change.Modify;
                case SvnChangeAction.Delete: return Change.Delete;
                case SvnChangeAction.Replace: return Change.Replace;
                default:
                    throw new Exception("Invalid SvnChangeAction: " + (int) action);
            }
        }

        public void AddDirectoryChildren(string path, int revision, Action<PathChange> action)
        {
            SvnClient client = AllocSvnClient();
            SvnTarget target = new SvnUriTarget(new Uri(uri + path), revision);
            try
            {
                SvnListArgs args = new SvnListArgs {Depth = SvnDepth.Infinity, Revision = revision};
                client.List(target, args, delegate(object s, SvnListEventArgs e)
                {
                    if (string.IsNullOrEmpty(e.Path)) return;
                    action(new PathChange {Change = Change.Add, Revision = revision, Path = e.BasePath + "/" + e.Path});
                });
            }
            finally
            {
                FreeSvnClient(client);
            }
        }

        public PathData GetPathData(string path, int revision)
        {
            SvnClient client = AllocSvnClient();
            SvnTarget target = new SvnUriTarget(new Uri(uri + path), revision);
            PathData data = null;
            try
            {
                SvnInfoEventArgs info;
                client.GetInfo(target, out info);

                data = new PathData();
                data.Path = path;
                data.Size = (int) info.RepositorySize;
                data.Author = info.LastChangeAuthor ?? "";
                data.Timestamp = info.LastChangeTime;
                data.Revision = (int) info.LastChangeRevision;
                data.FinalRevision = revision;
                data.IsDirectory = info.NodeKind == SvnNodeKind.Directory;

                Collection<SvnPropertyListEventArgs> pc;
                client.GetPropertyList(target, out pc);
                foreach (var proplist in pc)
                {
                    foreach (var property in proplist.Properties)
                    {
                        data.Properties.Add(property.Key, property.StringValue.ToLowerInvariant());
                    }
                }

                string mime;
                data.Properties.TryGetValue("svn:mime-type", out mime);
                const int MaxFileSize = 128*1024*1024;
                if (!data.IsDirectory && (string.IsNullOrEmpty(mime) || mime.StartsWith("text/")) &&
                    data.Size < MaxFileSize)
                {
                    MemoryStream stream = new MemoryStream(data.Size);
                    client.Write(target, stream);
                    stream.Position = 0;
                    data.Text = new StreamReader(stream).ReadToEnd();
                        // default utf-8 encoding, does not work with codepages
                    stream.Dispose();
                }
            }
            catch (SvnException x)
            {
                if (x.SvnErrorCode != SvnErrorCode.SVN_ERR_RA_ILLEGAL_URL) throw;
            }
                catch (Exception x)
                {
                    Console.WriteLine(x);
                }
            finally
            {
                FreeSvnClient(client);
            }
            return data;
        }
    }
}