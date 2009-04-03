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
using System.Linq;
using System.Text.RegularExpressions;

namespace SvnQuery
{
    public class IndexerArgsException: Exception
    {
         public IndexerArgsException(string msg): base(msg + Environment.NewLine + HelpMessage)
         {}

         const string HelpMessage = @"          
SvnIndex action index_path repository_uri [Options] 
  action := create | update
  Options:
  -r max revision to be included in the index
  -u User
  -p Password
  -f regex filter for items that should be ignored, e.g. "".*/tags/.*""
  -t max number of threads used to query the repository in parallel
  -c commit interval
  -o optimize interval
  -n name of the index (for display in clients e.g. SvnWebQuery)
  -x external visible repository uri 
  -v verbosity level (0..3, 0 is lowest, 1 is default)
";
    }

    public class IndexerArgs
    {
        public Indexer.Command Command;        
        public string IndexPath;
        public string RepositoryLocalUri;
        public string RepositoryExternalUri;
        public string RepositoryName;
        public string User;
        public string Password;
        public Regex Filter; // pathes that match this regex are not indexed
        public int MaxRevision = 99999999;
        public int MaxThreads; // default is initialized depending on protocol (file:///, svn:// http://) 
                                // as a general rule, the higher the latency the higher the number of threads should be
        public int Optimize = 25; // number of revisions that lead to optimization
        public int CommitInterval = 1000; // the interval between the index gets committed
        public int Verbosity; // Output data about the

        public IndexerArgs(string[] args)
        {
            if (args.Length == 0 || (args.Length == 1 && (args[0].EndsWith("?") || args[0].EndsWith("help", StringComparison.InvariantCultureIgnoreCase))))
                throw new IndexerArgsException("Usage:");

            int iMandatory = 0;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i][0] == '-')
                {
                    if (args[i].Length < 2) throw new IndexerArgsException("Empty Option");
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
                        case 'c':
                            CommitInterval = int.Parse(arg);
                            break;
                        case 'n':
                            RepositoryName = arg;
                            break;
                        case 'x':
                            RepositoryExternalUri = arg.Replace('\\', '/').TrimEnd('/');
                            break;
                        case 'v':
                            Verbosity = int.Parse(arg);
                            break;
                        default:
                            throw new IndexerArgsException("Unknown option -" + option);
                    }
                }
                else
                {
                    string arg = args[i];
                    switch (iMandatory++)
                    {
                        case 0: // first argument is the command
                            try
                            {
                                Command = (Indexer.Command) Enum.Parse(typeof (Indexer.Command), arg, true);
                            }
                            catch (ArgumentException)
                            {
                                throw new IndexerArgsException("Unknown command '" + arg + "'");
                            }
                            break;
                        case 1: // second comes the path to the index directory
                            IndexPath = Path.GetFullPath(arg);
                            break;
                        case 2: // third is the uri used to index the repository
                            RepositoryLocalUri = arg.Replace('\\', '/').TrimEnd('/');                            
                            break;
                    }
                }
            }
            if (iMandatory != 3) throw new IndexerArgsException("Not enough arguments");
            
            if (MaxThreads < 2) MaxThreads = GetMaxThreadsFromUri(RepositoryLocalUri);
        }

        static int GetMaxThreadsFromUri(string uri)
        {
            uri = uri.ToLowerInvariant();
            if (uri.StartsWith("http")) return 16;
            if (uri.StartsWith("svn")) return 8;
            if (uri.StartsWith("file") || Regex.IsMatch(uri, @"^[a-z]\:")) return 4; // local file repository            
            return 4; // unknown protocol
        }
    }
}