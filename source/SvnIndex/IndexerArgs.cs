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
using SvnQuery.Svn;

namespace SvnIndex
{
    public class IndexerArgsException : Exception
    {
        public IndexerArgsException(string msg) : base(msg + Environment.NewLine + HelpMessage)
        {}

        const string HelpMessage =
            @"          
SvnIndex action index_path repository_uri [Options] 
  action := create | update
  Options:
  -r max revision to be included in the index
  -f regex filter for items that should be ignored, default is ""/tags/""
    
  -n name of the index (for display in clients e.g. SvnWebQuery)
  -x external visible repository uri (if different than repository_uri)
  -v verbosity level (0..3, 0 lowest is 0, default is 1)

  -u User (only necessary for non local repositories)
  -p Password (only necessary for non local repositories)

  -t max number of threads used to query the repository in parallel
  -c commit interval
  -o optimize interval  

  -s create a solid single revision index (-r will set the revision)
";
    }

    public class IndexerArgs
    {
        public Indexer.Command Command;
        public string IndexPath;
        public string RepositoryLocalUri;
        public string RepositoryExternalUri;
        public string RepositoryName;
        public Credentials Credentials = new Credentials();
        public Regex Filter; // pathes that match this regex are not indexed
        public int MaxRevision = 99999999;
        public int MaxThreads; // default is initialized depending on protocol (file:///, svn:// http://) 
        // as a general rule, the higher the latency the higher the number of threads should be
        public int Optimize = 25; // number of revisions that lead to optimization
        public int CommitInterval = 1000; // the interval between the index gets committed
        public int Verbosity; // Verbosity of the index process 
        public bool SingleRevision;

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
                    try
                    {
                        switch (char.ToLowerInvariant(args[i][1])) // normalize option
                        {
                            case 'r':
                                MaxRevision = int.Parse(NextArg(args, ref i));
                                break;
                            case 'f':
                                Filter = new Regex(NextArg(args, ref i), RegexOptions.Compiled | RegexOptions.CultureInvariant);
                                break;
                            case 'u':
                                Credentials.User = NextArg(args, ref i);
                                break;
                            case 'p':
                                Credentials.Password = NextArg(args, ref i);
                                break;
                            case 't':
                                MaxThreads = int.Parse(NextArg(args, ref i));
                                break;
                            case 'o':
                                Optimize = int.Parse(NextArg(args, ref i));
                                break;
                            case 'c':
                                CommitInterval = int.Parse(NextArg(args, ref i));
                                break;
                            case 'n':
                                RepositoryName = NextArg(args, ref i);
                                break;
                            case 'x':
                                RepositoryExternalUri = NextArg(args, ref i).Replace('\\', '/').TrimEnd('/');
                                break;
                            case 'v':
                                Verbosity = int.Parse(NextArg(args, ref i));
                                break;
                            case 's':
                                SingleRevision = true;
                                break;
                            default:
                                throw new IndexerArgsException("Unknown option " + args[i]);
                        }
                    }
                    catch (Exception)
                    {
                        throw new IndexerArgsException("Invalid or missing argument for option " + args[i]);
                    }
                }
                else
                {
                    string arg = args[i];
                    switch (iMandatory++)
                    {
                        case 0: // first argument is the command
                            Command = ParseCommand(arg);
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
            if (Filter == null) Filter = new Regex("/tags/", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        static string NextArg(string[] args, ref int pos)
        {
            return (args[pos].Length == 2) ? args[++pos] : args[pos].Substring(2);
        }

        static Indexer.Command ParseCommand(string command)
        {
            try
            {
                return (Indexer.Command) Enum.Parse(typeof (Indexer.Command), command, true);
            }
            catch (ArgumentException)
            {
                throw new IndexerArgsException("Unknown command '" + command + "'");
            }
        }

        static int GetMaxThreadsFromUri(string uri)
        {
            uri = uri.ToLowerInvariant();
            if (uri.StartsWith("http")) return 16; // includes https
            if (uri.StartsWith("svn")) return 8;
            if (uri.StartsWith("file") || Regex.IsMatch(uri, @"^[a-z]\:")) return 4; // local file repository            
            return 4; // unknown protocol
        }
    }
}