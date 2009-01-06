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
using SharpSvn;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace SvnQuery
{
    public class SharpSvnApi : ISvnApi
    {
        readonly Uri uri;

        public SharpSvnApi(string repositoryUrl) : this(repositoryUrl, "", "")
        {}

        public SharpSvnApi(string repositoryUri, string user, string password)
        {
            uri = new Uri(repositoryUri);
        }

        public string User
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public string Password
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public int GetYoungestRevision()
        {
            SvnTarget target = new SvnUriTarget(uri);
            Console.WriteLine(uri);

            int youngest = 0;
            SvnClient client = new SvnClient();
            client.Info(target, delegate(object s, SvnInfoEventArgs e) { youngest = (int) e.Revision; });
            return youngest;
        }

        public void ForEachChange(int firstRevision, int lastRevision, Action<PathChange> callback)
        {
            using (SvnClient client = new SvnClient())
            {
                client.Log(uri, new SvnLogArgs(new SvnRevisionRange(firstRevision, lastRevision)), delegate(object s, SvnLogEventArgs e)
                {
                    if (e == null || e.ChangedPaths == null) return;
                    foreach (var path in e.ChangedPaths)
                    {
                        int revision = (int) e.Revision;
                        switch (path.Action)
                        {
                           case SvnChangeAction.Add:
                                callback(new PathChange(Change.Add, revision, path.Path, e.LogMessage));    
                               break;
                           case SvnChangeAction.Modify:
                               callback(new PathChange(Change.Modify, revision, path.Path, e.LogMessage)); 
                               break;
                           case SvnChangeAction.Delete:
                               callback(new PathChange(Change.Delete, revision, path.Path, e.LogMessage)); 
                               break;
                           case SvnChangeAction.Replace:
                               callback(new PathChange(Change.Delete, revision, path.Path, e.LogMessage));
                               callback(new PathChange(Change.Add, revision, path.Path, e.LogMessage));
                               break;
                       }
                    }
                });         
            }
        }

        public PathData GetPathData(string path, int revision)
        {
            Uri pathUri = new Uri(uri + path);
            SvnTarget target = new SvnUriTarget(pathUri, revision);

            using (SvnClient client = new SvnClient())
            {
                SvnInfoEventArgs info;
                client.GetInfo(target, out info);

                PathData data = new PathData();
                data.Size = (int) info.RepositorySize;
                data.Author = info.LastChangeAuthor;
                data.Timestamp = info.LastChangeTime;
                data.IsDirectory = info.NodeKind == SvnNodeKind.Directory;

                Collection<SvnPropertyListEventArgs> pc;
                client.GetPropertyList(target, out pc);
                foreach (var proplist in pc)
                {
                    foreach (var property in proplist.Properties)
                    {
                        data.Properties.Add(property.Key, property.StringValue);
                    }
                }

                string mime;
                data.Properties.TryGetValue("svn:mime-type", out mime);
                const int MaxFileSize = 128 * 1024 * 1024;
                if ((string.IsNullOrEmpty(mime) || mime.StartsWith("text/")) && data.Size < MaxFileSize)
                {
                    MemoryStream stream = new MemoryStream(data.Size);
                    client.Write(target, stream);
                    data.Text = new StreamReader(stream).ReadToEnd(); // default utf-8 encoding, does not work with codepages
                    stream.Dispose();
                }

                return data;
            }
        }

    }
}