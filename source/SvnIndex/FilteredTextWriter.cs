#region Apache License 2.0

// Copyright 2010 Christian Rodemeyer
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
using System.Text;

namespace SvnIndex
{
    /// <summary>
    /// Allows line-based filtering of a text writer.
    /// </summary>
    public class FilteredTextWriter : TextWriter
    {
        private Predicate<StringBuilder> _lineFilter;
        private StringBuilder _currentLine = new StringBuilder();
        private int _defaultLineLength = 0;

        public override Encoding Encoding
        {
            get { return BaseTextWriter.Encoding; }
        }

        public TextWriter BaseTextWriter { get; private set; }

        /// <summary>
        /// Default length of lines. If internal buffer gets twice as big it will be reduced to this length.
        /// </summary>
        public int DefaultLineLength
        {
            get { return _defaultLineLength; }
            set
            {
                if (_defaultLineLength != value)
                {
                    _defaultLineLength = value;
                    if (_currentLine.Capacity < _defaultLineLength)
                        _currentLine.Capacity = _defaultLineLength;
                }
            }
        }

        /// <summary>
        /// Contains maximum line lengh encountered so far.
        /// Can be used for diagnosis or for tweaking property DefaultLineLength.
        /// </summary>
        public int MaximumLineLength { get; private set; }

        public FilteredTextWriter(TextWriter baseTextWriter, Predicate<StringBuilder> lineFilter)
        {
            if (baseTextWriter == null)
                throw new ArgumentNullException("baseTextWriter");
            if (lineFilter == null)
                throw new ArgumentNullException("lineFilter");

            BaseTextWriter = baseTextWriter;
            _lineFilter = lineFilter;
        }

        public override void Write(char value)
        {
            _currentLine.Append(value);

            if (IsCompleteLine())
            {
                ProcessCurrentLine();
                CheckBufferCapacity();
            }
        }

        protected override void Dispose(bool pDisposing)
        {
            try
            {
                if (pDisposing)
                {
                    if (_currentLine.Length > 0)
                        // process last line
                        ProcessCurrentLine();
                }
            }
            finally
            {
                base.Dispose(pDisposing);
            }
        }

        private bool IsCompleteLine()
        {
            bool isNewLine = false;

            if (_currentLine.Length >= CoreNewLine.Length)
            {
                isNewLine = true;
                int indexNewLine = CoreNewLine.Length - 1;
                int indexLine = _currentLine.Length - 1;
                for (; isNewLine && indexNewLine >= 0; indexNewLine--, indexLine--)
                    isNewLine &= CoreNewLine[indexNewLine] == _currentLine[indexLine];
            }

            return isNewLine;
        }

        private void ProcessCurrentLine()
        {
            // update maximum line length if necessary
            if (MaximumLineLength < _currentLine.Length)
                MaximumLineLength = _currentLine.Length;

            // complete line found -> check if it should be passed to base text writer or not
            if (_lineFilter(_currentLine))
                BaseTextWriter.Write(_currentLine.ToString());
            // reset current line
            _currentLine.Length = 0;
        }

        private void CheckBufferCapacity()
        {
            if (DefaultLineLength > 0)
            {
                // trim string builder capacity if desired
                if (_currentLine.Capacity > DefaultLineLength * 2)
                    _currentLine.Capacity = DefaultLineLength;
            }
        }
    }
}
