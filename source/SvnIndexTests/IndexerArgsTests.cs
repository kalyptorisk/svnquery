
using NUnit.Framework;
using SvnIndex;

namespace SvnIndexTests
{
    [TestFixture]
    public class IndexerArgsTests
    {
        [Test]
        public void IgnoreBinary_IsGiven_ShouldReturnGivenRegex()
        {
            var args = new IndexerArgs(Args(@"create index repository -b-(c|cpp)$"));
            Assert.AreEqual("-(c|cpp)$", args.IgnoreBinaryFilter.ToString());
        }

        [Test]
        public void IgnoreBinary_NotGiven_ShouldForceCSharpFiles()
        {
            var args = new IndexerArgs(Args("create IndexerArgs repository"));
            Assert.IsFalse(args.IgnoreBinaryFilter.IsMatch(".csharp"));
            Assert.IsTrue(args.IgnoreBinaryFilter.IsMatch(".cs"));
        }

        [Test]
        public void SingleRevisionOption_IsTrue()
        {
            var args = new IndexerArgs(Args(@"create d:\index d:\repository -r1000 -s -v 4"));

            Assert.IsTrue(args.SingleRevision);
            Assert.AreEqual(4, args.Verbosity);
            Assert.AreEqual(1000, args.MaxRevision);
        }

        [Test]
        public void MissingOptionArgument_IndexerArgsException()
        {
            try
            {
                new IndexerArgs(Args(@"create d:\index d:\repository -r -v 4"));
                Assert.Fail("IndexerArgsException expected");
            }
            catch (IndexerArgsException)
            {
                Assert.IsTrue(true);
            }
        }

        static string[] Args(string args)
        {
            return args.Split(' ');
        }

    }
}