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
        public void CommitInternval_50_37_1233_ExpectedResult()
        {
            const int startRev = 37;
            const int stopRev = 1233;
            const int interval = 50;

            for (int i = startRev; i <= stopRev; i += interval)
            {
                Console.WriteLine("{0} - {1}", i, Math.Min(i + interval - 1, stopRev));
            }

        }
    }
}
