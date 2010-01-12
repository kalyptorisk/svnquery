using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace SvnQuery.Tests
{
    [TestFixture]
    public class CredentialsTest: AssertionHelper
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
