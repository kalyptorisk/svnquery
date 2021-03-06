﻿#region Apache License 2.0

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

namespace SvnQuery.Lucene
{
    /// <summary>
    /// FieldNames for the Lucene Index
    /// </summary>
    public static class FieldName
    {
        public const string Author = "author";
        public const string Content = "content";
        public const string Externals = "externals";
        public const string Id = "id";
        public const string IsRevision = "is_revision";
        public const string Message = "message";
        public const string MimeType = "mime";
        public const string Path = "path";
        public const string RevisionFirst = "rev_first";
        public const string RevisionLast = "rev_last";
        public const string Size = "size";
        public const string Timestamp = "timestamp";

    }
}