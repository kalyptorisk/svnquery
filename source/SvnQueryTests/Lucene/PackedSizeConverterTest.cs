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

using NUnit.Framework;
using SvnQuery.Lucene;

namespace SvnQuery.Tests.Lucene
{
    [TestFixture]
    public class PackedSizeConverterTest
    {
        const int kb = 1024;
        const int mb = 1024*kb;
        const int gb = 1024*mb;

        [Test]
        public void ToSortableString_Bytes()
        {
            Assert.AreEqual("b000", PackedSizeConverter.ToSortableString(0));
            Assert.AreEqual("b3E7", PackedSizeConverter.ToSortableString(999));
            Assert.AreEqual("b3FF", PackedSizeConverter.ToSortableString(kb - 1));
            Assert.AreEqual("k001", PackedSizeConverter.ToSortableString(kb));
        }

        [Test]
        public void ToSortableString_KBytes()
        {
            Assert.AreEqual("k001", PackedSizeConverter.ToSortableString(kb));
            Assert.AreEqual("k3FF", PackedSizeConverter.ToSortableString(mb - 1));
            Assert.AreEqual("m001", PackedSizeConverter.ToSortableString(mb));
        }

        [Test]
        public void ToSortableString_MBytes()
        {
            Assert.AreEqual("m001", PackedSizeConverter.ToSortableString(mb));
            Assert.AreEqual("m3FF", PackedSizeConverter.ToSortableString(gb - 1));
            Assert.AreEqual("z001", PackedSizeConverter.ToSortableString(gb));
        }

        [Test]
        public void FromSortableString()
        {
            Assert.AreEqual(999, PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(999)));
            Assert.AreEqual(999, PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(999*kb)) / kb);
            Assert.AreEqual(999, PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(999*mb)) / mb);
            Assert.AreEqual(1, PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(1*gb)) / gb);
        }
    }
}