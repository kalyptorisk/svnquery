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

namespace SvnQuery
{
    public interface ISvnApi
    {
        /// <summary>
        /// Gets the highest revision number of the repository
        /// </summary>
        int GetYoungestRevision();

        /// <summary>
        /// returns the uuid of the repository
        /// </summary>        
        Guid GetRepositoryId();

        /// <summary>
        /// Gets some revision data and all changes in a revision
        /// </summary>
        List<RevisionData> GetRevisionData(int firstRevision, int lastRevision);

        /// <summary>
        /// Lists a folder recursively with PathChanges of type add
        /// </summary>
        void AddDirectoryChildren(string path, int revision, Action<PathChange> action);

        /// <summary>
        /// Gets data for a path in a given revision
        /// </summary>
        PathData GetPathData(string path, int revision);

        /// <summary>
        /// Gets the revision comment for a given revision 
        /// </summary>
        string GetLogMessage(int revision);
    }

    public enum Change
    {
        Add,
        Modify,
        Delete,
        Replace
    }

    public class RevisionData
    {
        public int Revision;
        public string Author;
        public DateTime Timestamp;
        public string Message;
        public List<PathChange> Changes = new List<PathChange>();
    }

    public class PathChange
    {
        public Change Change;
        public int Revision;
        public string Path;
        public bool IsCopy; //  if IsCopy and IsDirectory than children need to be added explicitely

        public override string ToString()
        {
            return Change + " " + Path + "@" + Revision;
        }
    }

    public class PathData
    {
        public string Path;
        public int Revision;
        public int FinalRevision;
        public string Author;
        public DateTime Timestamp;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        public int Size;
        public bool IsDirectory;
        public string Text; // null if binary or directory
    }
}