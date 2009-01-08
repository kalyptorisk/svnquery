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
using System.Diagnostics;

namespace SvnQuery
{
   
    public interface ISvnApi
    {
        int GetYoungestRevision();

        void ForEachChange(int firstRevision, int lastRevision, Action<PathChange> callback);

        /// <summary>
        /// Lists a folder recursively with PathChanges of type add
        /// </summary>
        void ForEachChild(string path, int revision, Action<PathChange> callback);

        PathData GetPathData(string path, int revision);

        string GetLogMessage(int revision);
    }

    public enum Change
    {
        Add, Modify, Delete, Replace
    }

    public class PathChange
    {
        public Change Change;
        public int Revision;
        public string Path;
        public bool IsCopy; //  if IsCopy and IsDirectory than children need to be added explicitely

#if DEBUG
        public override string ToString()
        {
            return Change + " " + Path + "@" + Revision;
        }
#endif 
    }

    public class PathData
    {
        public string Path;
        public int FirstRevision;
        public string Author;
        public DateTime Timestamp;
        public bool IsDirectory;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();        
        public string Text; // null if binary
        public int Size;
        public int LastRevision;
    } 
    
}