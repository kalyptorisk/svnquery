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

namespace SvnQuery.Tests
{
    [TestFixture]
    public class PackedSizeConverterTest
    {
        [Test]
        public void ToSortableString_Bytes()
        {
            Assert.AreEqual("b000", PackedSizeConverter.ToSortableString(0));
            Assert.AreEqual("b3E7", PackedSizeConverter.ToSortableString(999));
            Assert.AreEqual("b3FF", PackedSizeConverter.ToSortableString(1023));
            Assert.AreEqual("k001", PackedSizeConverter.ToSortableString(1024));
        }

        [Test]
        public void ToSortableString_KBytes()
        {
            Assert.AreEqual("k001", PackedSizeConverter.ToSortableString(1024));
            Assert.AreEqual("k3FF", PackedSizeConverter.ToSortableString(1024*1024 - 1));
            Assert.AreEqual("m001", PackedSizeConverter.ToSortableString(1024*1024));
        }

        [Test]
        public void ToSortableString_MBytes()
        {
            Assert.AreEqual("m001", PackedSizeConverter.ToSortableString(1024*1024));
            Assert.AreEqual("m3FF", PackedSizeConverter.ToSortableString(1024*1024*1024 - 1));
            Assert.AreEqual("z001", PackedSizeConverter.ToSortableString(1024*1024*1024));
        }

        [Test]
        public void FromSortableString()
        {
            Assert.AreEqual(999, PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(999)));
            Assert.AreEqual(999*1024,
                            PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(999*1024)));
            Assert.AreEqual(999*1024*1024,
                            PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(999*1024*1024)));
            Assert.AreEqual(1*1024*1024*1024,
                            PackedSizeConverter.FromSortableString(PackedSizeConverter.ToSortableString(1*1024*1024*1024)));
        }
    }
}