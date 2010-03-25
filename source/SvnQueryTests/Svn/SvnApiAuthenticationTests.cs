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
using SvnQuery.Svn;

namespace SvnQuery.Tests.Svn
{
    [TestFixture]
    public class SvnApiAuthenticationTests
    {
        [Test, Ignore]
        public void RepositoryNeedsAuthentication_Correct_Success()
        {
            SharpSvnApi api = new SharpSvnApi("http://sharpsvn.open.collab.net/svn/sharpsvn/trunk", "crodemeyer",
                                              "*********");
            api.GetYoungestRevision();
            Assert.That(true);
        }

        [Test, Ignore]
        public void RepositoryNeedsAuthentication_WrongPassword_Failure()
        {
            try
            {
                SharpSvnApi api = new SharpSvnApi("http://sharpsvn.open.collab.net/svn/sharpsvn/trunk", "crodemeyer",
                                                  "wrong");
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