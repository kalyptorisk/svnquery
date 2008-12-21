#region Apache License 2.0
//
// Copyright 2008 Christian Rodemeyer
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;

namespace SvnQuery
{
    public class ContentFromRepository : ContentTokenStream
    {
        const string BinaryContent = "%BINARY%";

        readonly Process svnlook = new Process();
        readonly string propget;
        readonly string cat;

        string mime;
        bool isText;
        bool hasBinaryToken;
                                        
        public ContentFromRepository(string repository)
        {
            svnlook.StartInfo.RedirectStandardError = true;
            svnlook.StartInfo.RedirectStandardOutput = true;
            svnlook.StartInfo.UseShellExecute = false;
            svnlook.StartInfo.ErrorDialog = false;
            svnlook.StartInfo.CreateNoWindow = true;
            svnlook.StartInfo.FileName = "svnlook";

            // not sure if this is the right way, but otherwise
            // german umlaute in old c++ files become garbled
            svnlook.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(1252);

            propget = string.Format("propget {0} svn:mime-type -r", repository);
            cat = string.Format("cat {0} -r", repository);
        }

        public bool Reset(string path, string revision)
        {
            if (path.EndsWith("/")) // folders have no content
            {
                isText = false;
                hasBinaryToken = false;
                return false;
            }
           
            mime = ReadMimeType(path, revision);
            isText = (string.IsNullOrEmpty(mime) || mime.StartsWith("text/"));
            hasBinaryToken = !isText;
            SetReader(OpenReader(path, revision));
            return true;           
        }

        string ReadMimeType(string path, string revision)
        {
            if (svnlook.StartInfo.Arguments != "") // was started
            {
                svnlook.StandardOutput.ReadToEnd();
                svnlook.StandardError.ReadToEnd();
            }

            svnlook.StartInfo.Arguments = propget + revision + " \"" + path + "\"";
            svnlook.Start();
            string mime_string = svnlook.StandardOutput.ReadToEnd();
            svnlook.StandardError.ReadToEnd();
            Debug.Assert(svnlook.HasExited);
            return mime_string;
        }

        TextReader OpenReader(string path, string revision)
        {
            if (svnlook.StartInfo.Arguments != "")
            {
                svnlook.StandardOutput.ReadToEnd();
                svnlook.StandardError.ReadToEnd();
            }
            svnlook.StartInfo.Arguments = cat + revision + " \"" + path + "\"";
            svnlook.Start();
            return svnlook.StandardOutput;
        }

        public override Token Next(Token token)
        {
            if (isText) return base.Next(token);
            if (!hasBinaryToken) return null;

            BinaryContent.CopyTo(0, token.TermBuffer(), 0, BinaryContent.Length);
            token.SetStartOffset(0);
            token.SetEndOffset(BinaryContent.Length - 1);
            token.SetTermLength(BinaryContent.Length);
            return token;
        }

    }

}