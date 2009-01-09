

using NUnit.Framework;

using SvnQuery;

namespace SvnIndexTests
{
    [TestFixture]
    public class SvnApiAuthenticationTests
    {

        [Test, Ignore]
        public void RepositoryNeedsAuthentication_Correct_Success()
        {
            SharpSvnApi api = new SharpSvnApi("http://sharpsvn.open.collab.net/svn/sharpsvn/trunk", "crodemeyer", "*********");
            api.GetYoungestRevision();
            Assert.That(true);
        }

        [Test, Ignore]
        public void RepositoryNeedsAuthentication_WrongPassword_Failure()
        {
            try
            {
                SharpSvnApi api = new SharpSvnApi("http://sharpsvn.open.collab.net/svn/sharpsvn/trunk", "crodemeyer", "wrong");
                api.GetYoungestRevision();
            }
            catch 
            {
                return;
            }
            Assert.That(false); // Exception expected
        } 
        
        [Test, Ignore]
        public void RepositoryNeedsAuthentication_NoPassword_Failure()
        {
            try
            {
                SharpSvnApi api = new SharpSvnApi("http://sharpsvn.open.collab.net/svn/sharpsvn/trunk");
                api.GetYoungestRevision();
            }
            catch 
            {
                return;
            }
            Assert.That(false); // Exception expected
        }

    }
}