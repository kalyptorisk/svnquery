using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SvnIndexTests
{
    [TestFixture]
    public class MiscTests
    {
        [Test]
        public void CommitInterval_50_37_1233_ExpectedResult()
        {
            const int startRev = 37;
            const int stopRev = 1233;
            const int interval = 50;

            List<int> list = new List<int>();
            for (int i = startRev; i <= stopRev; i += interval)
            {
                int first = i;
                int last = Math.Min(first + interval - 1, stopRev);
                Console.WriteLine("{0} - {1}", first, last);
                list.Add(first);
                list.Add(last);
            }
            Assert.That(list.First() == startRev);
            Assert.That(list.Last() == stopRev);
            Assert.That(list.Count / 2 == (int)Math.Ceiling((stopRev - startRev) / (double)interval));

        }
    }
}
