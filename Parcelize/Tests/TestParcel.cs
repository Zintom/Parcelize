using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Zintom.Parcelize.Tests
{
    [TestClass]
    public class TestParcel
    {
        [TestMethod]
        public void TestRoundtrip()
        {
            Parcel p = new();
            p.WriteInt(1);
            p.WriteParcel(new MockParcelable().ToParcel());
            p.WriteInt(3);

            byte[] bytes = p.ToBytes();

            Parcel p2 = Parcel.FromBytes(bytes);

            Assert.IsTrue(p2.ReadNext<int>() == 1);
            Assert.IsTrue(p2.ReadNext<Parcel>() != null);
            Assert.IsTrue(p2.ReadNext<int>() == 3);
        }

        private class MockParcelable : IParcelable
        {
            public Parcel ToParcel() => new();
        }

        [TestMethod]
        public void TestNullItemRoundtrip()
        {
            Parcel p = new();
            p.WriteInt(1);
            p.WriteParcel(null);
            p.WriteInt(3);

            byte[] bytes = p.ToBytes();

            Parcel p2 = Parcel.FromBytes(bytes);

            Assert.IsTrue(p2.ReadNext<int>() == 1);
            Assert.IsTrue(p2.ReadNext<Parcel>() == null);
            Assert.IsTrue(p2.ReadNext<int>() == 3);
        }
    }
}
