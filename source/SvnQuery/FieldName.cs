

namespace SvnQuery
{
    /// <summary>
    /// FieldNames for the Lucene Index
    /// </summary>
    public static class FieldName
    {
        public const string Id = "id";
        public const string RevisionFirst = "rev_first";
        public const string RevisionLast = "rev_last";
        public const string Size = "size";
        public const string Timestamp = "timestamp";
        public const string Author = "author";
        public const string Message = "message";
        public const string Path = "path";
        public const string Content = "content";
        public const string Externals = "externals";
        public const string Type = "type";
        
    }
}
