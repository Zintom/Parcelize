using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zintom.Parcelize.Helpers;

namespace Zintom.Parcelize.Tests
{
    [TestClass]
    public class TestArrayHelpers
    {
        [TestMethod]
        public void CombineArrays()
        {
            byte[] one = new byte[1] { 1 };
            byte[] two = new byte[1] { 2 };
            byte[] three = new byte[1] { 3 };

            byte[] combined = ArrayHelpers.CombineArrays(one, two, three);

            Assert.IsTrue(combined[0] == 1);
            Assert.IsTrue(combined[1] == 2);
            Assert.IsTrue(combined[2] == 3);
            Assert.IsTrue(combined.Length == 3);
        }
    }
}
