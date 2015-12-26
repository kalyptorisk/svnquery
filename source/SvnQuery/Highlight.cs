#region Apache License 2.0

// Copyright 2015 Ashish Kulkarni
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
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Highlight;
using Lucene.Net.Search;
using SvnQuery.Lucene;
using SvnQuery.Svn;

namespace SvnQuery
{
    public abstract class Highlight
    {
        public ISvnApi Svn { get; private set; }

        public Highlight(ISvnApi svn)
        {
            Svn = svn;
        }

        public string[] GetFragments(Query query, Hits hits)
        {
            var highlighter = CreateHighlighter(query);
            var result      = new string[hits.Length()];
            for (var i=0; i<result.Length; i++)
            {
                var size   = PackedSizeConverter.FromSortableString(hits.Doc(i).Get(FieldName.Size));
                var loc    = hits.Doc(i).Get(FieldName.Id).Split('@');
                var info   = Svn.GetPathInfo(loc[0], Convert.ToInt32(loc[1]));
                if (info.IsDirectory)
                    continue;

                var text   = Svn.GetPathContent(loc[0], Convert.ToInt32(loc[1]), size);
                var tokens = new SimpleTokenStream(text);
                result[i]  = GetFragments(highlighter, tokens, text);
            }
            return result;
        }

        protected virtual Highlighter CreateHighlighter(Query query)
        {
            var highlighter = new Highlighter(new QueryScorer(query));
            highlighter.SetTextFragmenter(CreateFragmenter());
            highlighter.SetMaxDocBytesToAnalyze(int.MaxValue);
            highlighter.SetEncoder(new SimpleHTMLEncoder());
            return highlighter;
        }

        protected abstract Fragmenter CreateFragmenter();
        protected abstract string GetFragments(Highlighter highlighter, TokenStream stream, string text);
    }

    public class SimpleHighlight : Highlight
    {
        private const int FRAGMENT_SIZE = 100;
        private const int NUM_FRAGMENTS = 3;
        private const string SEPARATOR  = " ... ";

        private int maxNumFragments, fragmentSize;
        private string separator;

        public SimpleHighlight(ISvnApi svn)
            : this(svn, NUM_FRAGMENTS, SEPARATOR, FRAGMENT_SIZE)
        { }

        public SimpleHighlight(ISvnApi svn, int maxNumFragments)
            : this(svn, maxNumFragments, SEPARATOR, FRAGMENT_SIZE)
        { }

        public SimpleHighlight(ISvnApi svn, int maxNumFragments, string separator)
            : this(svn, maxNumFragments, separator, FRAGMENT_SIZE)
        { }

        public SimpleHighlight(ISvnApi svn, int maxNumFragments, string separator, int fragmentSize) : base(svn)
        {
            this.maxNumFragments = maxNumFragments;
            this.separator       = separator;
            this.fragmentSize    = fragmentSize;
        }

        protected override Fragmenter CreateFragmenter()
        {
            return new SimpleFragmenter(fragmentSize);
        }

        protected override string GetFragments(Highlighter highlighter, TokenStream stream, string text)
        {
            return highlighter.GetBestFragments(stream, text, maxNumFragments, separator);
        }
    }

    public class LineHighlight : Highlight
    {
        private const int NUM_FRAGMENTS = 3;
        private const int CONTEXT_LINES = 2;
        private static readonly char[] DELIMITERS = "\r\n".ToCharArray();

        public class LineFragmenter : Fragmenter
        {
            private string text;
            private int offset;

            public bool IsNewFragment(Token nextToken)
            {
                int current = nextToken.StartOffset(), previous = offset;
                offset = current;
                return text.IndexOfAny(DELIMITERS, previous, current-previous) != -1;
            }

            public void Start(string originalText)
            {
                text = originalText;
                offset = 0;
            }
        }

        private int maxNumFragments, contextLines;
        public LineHighlight(ISvnApi svn) : this(svn, NUM_FRAGMENTS, CONTEXT_LINES)
        { }

        public LineHighlight(ISvnApi svn, int maxNumFragments) : this(svn, maxNumFragments, CONTEXT_LINES)
        { }

        public LineHighlight(ISvnApi svn, int maxNumFragments, int contextLines) : base(svn)
        {
            this.maxNumFragments = maxNumFragments;
            this.contextLines    = contextLines;
        }

        protected override Fragmenter CreateFragmenter()
        {
            return new LineFragmenter();
        }

        protected override string GetFragments(Highlighter highlighter, TokenStream stream, string text)
        {
            var fragments = Array.FindAll(highlighter.GetBestTextFragments(stream, text, false, maxNumFragments), x => x.GetScore() > 0);
            if (fragments.Length == 0)
                return string.Empty;

            var markupText = fragments[0].markedUpText.ToString();
            var matches    = Regex.Matches(markupText, "\r\n|\r|\n");
            var offsets    = new int[1+matches.Count];

            offsets[0] = 0;
            for (var i=0; i<matches.Count; i++)
                offsets[1+i] = matches[i].Index+matches[i].Length;

            var result = new System.Text.StringBuilder();
            var lines  = new System.Collections.Generic.List<int>();
            foreach (var fragment in fragments)
            {
                var offset = markupText.LastIndexOfAny(DELIMITERS, fragment.textEndPos-1, fragment.textEndPos-fragment.textStartPos);
                var line   = Array.FindIndex(offsets, x => x >= (offset == -1 ? fragment.textStartPos : offset));

                for (var l=Math.Max(0, line-contextLines); l<=Math.Min(matches.Count, line+contextLines); l++)
                    if (!lines.Contains(l))
                        lines.Add(l);
            }
            lines.Sort();

            var strFormat = "D"+lines[lines.Count-1].ToString().Length;
            for (var i=0; i<lines.Count; i++)
            {
                var line = lines[i];
                if (i > 0 && lines[i] > 1+lines[i-1])
                    result.AppendLine(" .. ");
                result.Append("<span class='line'>L")
                      .Append((1+line).ToString(strFormat))
                      .Append(": </span>")
                      .Append(markupText, offsets[line], (line == matches.Count ? markupText.Length : offsets[line+1]) - offsets[line]);
            }
            return result.ToString();
        }
    }
}
