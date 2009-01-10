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
using System.Globalization;

namespace SvnQuery
{
    public static class PackedSizeConverter
    {
        const int kb = 1024;
        const int mb = 1024*1024;
        const int gb = 1024*1024*1024;

        public static string ToSortableString(int size)
        {
            if (size < kb) return "b" + size.ToString("X3");
            if (size < mb) return "k" + (size/kb).ToString("X3");
            if (size < gb) return "m" + (size/mb).ToString("X3");

            return "z001";
        }

        public static string ToString(int size)
        {
            if (size < kb) return size + "b";
            if (size < mb) return (size/kb) + "kb";
            if (size < gb) return (size/mb) + "mb";

            return (size/gb) + "gb";
        }

        public static int FromSortableString(string size)
        {
            int v = int.Parse(size.Substring(1), NumberStyles.HexNumber);
            switch (size[0])
            {
                case 'b':
                    return v;
                case 'k':
                    return v*kb;
                case 'm':
                    return v*mb;
                case 'z':
                    return v*gb;
            }
            throw new ArgumentException("size is not a packed size");
        }

        public static string FromSortableStringToString(string size)
        {
            return ToString(FromSortableString(size));
        }
    }
}