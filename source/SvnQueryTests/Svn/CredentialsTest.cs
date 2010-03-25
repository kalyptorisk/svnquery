#region Apache License 2.0

// Copyright 2008-2010 Christian Rodemeyer
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
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using SvnQuery.Svn;

namespace SvnQuery.Tests.Svn
{
    [TestFixture]
    public class CredentialsTest : AssertionHelper
    {
        [Test]
        public void ToString_Credentials_Unreadable()
        {
            // Arrange 
            var credentials = new Credentials();
            credentials.User = "Blubber";
            credentials.Password = "Quatsch{48}Ä";

            // Act 
            string data = credentials.ToString();

            // Assert            
            Expect(data, Not.Contains(credentials.User));
            Expect(data, Not.Contains(credentials.Password));
        }

        [Test]
        public void Roundtrip_UserAndPassword_SameAsOriginal()
        {
            // Arranged
            var original = new Credentials();
            original.User = "Blubber";
            original.Password = "Quatsch{48}Ä";
            string data = original.ToString();

            // Act
            var credentials = new Credentials(data);

            // Assert
            Expect(credentials.User, Is.EqualTo(original.User));
            Expect(credentials.Password, Is.EqualTo(original.Password));
        }
    }
}