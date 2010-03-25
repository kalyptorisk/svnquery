
using NUnit.Framework;
using SvnIndex;

namespace SvnIndexTests
{
    [TestFixture]
    public class IndexerArgsTests
    {

        [Test]
        public void SingleRevisionOption_IsTrue()
        {
            var args = new IndexerArgs(@"create d:\index d:\repository -r1000 -s -v 4".Split(' '));

            Assert.IsTrue(args.SingleRevision);
            Assert.AreEqual(4, args.Verbosity);
            Assert.AreEqual(1000, args.MaxRevision);
        }

        [Test]
        public void MissingOptionArgument_IndexerArgsException()
        {
            try
            {
                new IndexerArgs(@"create d:\index d:\repository -r -v 4".Split(' '));
                Assert.Fail("IndexerArgsException expected");
            }
            catch (IndexerArgsException)
            {
                Assert.IsTrue(true);
            }
        }
    }
}