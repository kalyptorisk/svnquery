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
using System.IO;
using System.Text.RegularExpressions;

namespace SvnQuery
{
    public class IndexerArgs
    {
        public Indexer.Command Command;
        public string IndexPath;
        public string RepositoryUri;
        public string User;
        public string Password;
        public Regex Filter; // pathes that match this regex are not indexed
        public int MaxRevision = 99999999;
        public int MaxThreads = 25;
        public int Optimize = 25; // number of revisions that lead to optimization

        public IndexerArgs(string[] args)
        {
            bool allMandatoryArgumentsFound = false;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i][0] == '-')
                {
                    if (args[i].Length < 2) throw new Exception("Empty Option");
                    char option = char.ToLowerInvariant(args[i][1]);
                    string arg = (args[i].Length == 2) ? args[++i] : args[i].Substring(2);
                    switch (option)
                    {
                        case 'r':
                            MaxRevision = int.Parse(arg);
                            break;
                        case 'f':
                            Filter = new Regex(arg, RegexOptions.Compiled);
                            break;
                        case 'u':
                            User = arg;
                            break;
                        case 'p':
                            Password = arg;
                            break;
                        case 't':
                            MaxThreads = int.Parse(arg);
                            break;
                        case 'o':
                            Optimize = int.Parse(arg);
                            break;
                        default:
                            throw new Exception("Unknown option -" + option);
                    }
                }
                else
                    switch (i)
                    {
                        case 0:
                            Command = (Indexer.Command) Enum.Parse(typeof (Indexer.Command), args[i], true);
                            break;
                        case 1:
                            IndexPath = Path.GetFullPath(args[i]);
                            break;
                        case 2:
                            RepositoryUri = args[i];
                            allMandatoryArgumentsFound = true;
                            break;
                    }
            }
            if (!allMandatoryArgumentsFound) throw new Exception("Missing arguments");
        }
    }
}