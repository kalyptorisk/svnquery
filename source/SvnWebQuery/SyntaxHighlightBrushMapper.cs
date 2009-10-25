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
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace SvnWebQuery
{
    /// <summary>
    /// Maps a file extension to a language to be syntax highlighted.
    /// </summary>
    public class SyntaxHighlightBrushMapper
    {
        // script <- class <- extension 
        static readonly string[,] BrushToExtensions =
            {
                {"AS3", "as3"},
                //{"Bash", "bash"},
                {"ColdFusion", "(cfm)|(cfml)"},
                {"Cpp", "(c)|(h)|(hpp)|(cpp)|(inl)"},
                {"CSharp", "cs"},
                {"Css", "css"},
                {"Delphi", "pas"},
                {"Diff", "(diff)|(patch)"},
                {"Erlang", ".rl"},
                {"Groovy", "groovy"},
                {"Java", "java"},
                {"JavaFX", "jvx"},
                {"JScript", "js"},
                {"Perl", "(perl)|(pl)"},
                {"Php", "php"},
                {"PowerShell", "ps1"},
                {"Python", "py"},
                {"Ruby", "(rb)|(ruby)"},
                {"Scala", "(scl)|(scala)"},
                {"Sql", "sql"},
                {"Vb", "vb"},
                {"Xml", "(.*ml)|(.*proj)|(targets)"}, // interpreting msbuild projects and targets as xml

                {"Plain", ".*"}, // matches everything, must be the last in the list
            };

        readonly string _brush;

        public SyntaxHighlightBrushMapper(string path)
        {
            string ext = path.Substring(path.LastIndexOf('.') + 1);

            for (int i = 0; i <= BrushToExtensions.GetUpperBound(0); ++i)
            {
                if (Regex.IsMatch(ext, "^(" + BrushToExtensions[i, 1] + ")$", RegexOptions.IgnoreCase))
                {
                    _brush = BrushToExtensions[i, 0];
                    break;
                }
            }
        }

        /// <summary>
        /// True if a syntax highlighting brush is available
        /// </summary>
        public bool IsAvailable
        {
            get { return _brush != null; }
        }

        /// <summary>
        /// gets the syntax highlighting class 
        /// </summary>
        public string GetClass()
        {
            if (_brush == null) return "";
            return "brush: " + _brush.ToLowerInvariant() + ";";
        }

        /// <summary>
        /// gets the syntax highlighting script
        /// </summary>        
        public string GetScript()
        {
            if (_brush == null) return "";
            return "shBrush" + _brush + ".js";
        }
    }
}