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
        string User { get; set; }
        string Password { get; set; }

        int GetYoungestRevision();

        void ForEachChange(int firstRevision, int lastRevision, Action<PathChange> callback);

        PathData GetPathData(string path, int revision);        
    }

    public enum Change
    {
        Add, Modify, Delete
    }

    public class PathChange
    {
        public readonly Change Change;
        public readonly int Revision;
        public readonly string Path;
        public readonly string Message;

        public PathChange(Change change, int revision, string path, string message)
        {
            Change = change;
            Path = path;
            Message = message;
            Revision = revision;
        }
    }

    public class PathData
    {
        public string Author;
        public DateTime Timestamp;
        public bool IsDirectory;
        public Dictionary<string, string> Properties = new Dictionary<string, string>();        
        public string Text; // null if binary
        public int Size;
    } 
    
}