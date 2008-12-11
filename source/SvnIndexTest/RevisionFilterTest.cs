using NUnit.Framework;

namespace SvnIndexTest
{
    [TestFixture]
    public class RevisionFilterTest
    {
        [Test]
        public void HeadRevisionExist_Flip_cs()
        {
            TestIndex.AssertQuery("p:flip.cs", 14);
        }

        [Test]
        public void DeletedFile_DoesNotExistInHeadRevision()
        {
            TestIndex.AssertQuery("p:deleted.cpp");
        }

        [Test]
        public void FindFirstRevision()
        {
            TestIndex.AssertQueryFromRevision(1, "p:bla.cpp", 21);
            TestIndex.AssertQueryFromRevision(3, "p:bla.cpp", 21);
            TestIndex.AssertQueryFromRevision(5, "p:bla.cpp", 21);
        }

        [Test]
        public void FindBorderRevision()
        {
            TestIndex.AssertQueryFromRevision(6, "p:bla.cpp", 22);
            TestIndex.AssertQueryFromRevision(7, "p:bla.cpp", 23);
            TestIndex.AssertQueryFromRevision(8, "p:bla.cpp", 23);
        }

        [Test]
        public void DeletedFile_FoundInRevision6to8()
        {
            TestIndex.AssertQueryFromRevision(int.MaxValue, "p:deleted.cpp"); // HEAD should find nothing   
            TestIndex.AssertQueryFromRevision(0, "p:deleted.cpp", 25); // ALL should find it
            TestIndex.AssertQueryFromRevision(5, "p:deleted.cpp"); 
            TestIndex.AssertQueryFromRevision(6, "p:deleted.cpp", 25); 
            TestIndex.AssertQueryFromRevision(7, "p:deleted.cpp", 25); 
            TestIndex.AssertQueryFromRevision(8, "p:deleted.cpp", 25); 
            TestIndex.AssertQueryFromRevision(9, "p:deleted.cpp"); 
        }
        
        [Test]
        public void RangeQuery4to8()
        {
            TestIndex.AssertQueryFromRevisionRange(4, 8, ".cpp", 21, 22, 23, 25);
        }
    }
}