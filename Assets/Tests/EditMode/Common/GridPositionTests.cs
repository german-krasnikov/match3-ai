// Assets/Tests/EditMode/Common/GridPositionTests.cs
using NUnit.Framework;
using Common;

namespace Common
{
    [TestFixture]
    public class GridPositionTests
    {
        [Test]
        public void Constructor_SetsCoordinates()
        {
            var pos = new GridPosition(3, 5);

            Assert.AreEqual(3, pos.X);
            Assert.AreEqual(5, pos.Y);
        }

        [Test]
        public void Invalid_ReturnsNegativeCoordinates()
        {
            var invalid = GridPosition.Invalid;

            Assert.AreEqual(-1, invalid.X);
            Assert.AreEqual(-1, invalid.Y);
        }

        [Test]
        public void IsValid_WhenPositive_ReturnsTrue()
        {
            var pos = new GridPosition(0, 0);

            Assert.IsTrue(pos.IsValid);
        }

        [Test]
        public void IsValid_WhenNegative_ReturnsFalse()
        {
            Assert.IsFalse(new GridPosition(-1, 0).IsValid);
            Assert.IsFalse(new GridPosition(0, -1).IsValid);
        }

        [Test]
        public void Equals_SameCoordinates_ReturnsTrue()
        {
            var pos1 = new GridPosition(2, 3);
            var pos2 = new GridPosition(2, 3);

            Assert.IsTrue(pos1.Equals(pos2));
            Assert.IsTrue(pos1 == pos2);
        }

        [Test]
        public void Equals_DifferentCoordinates_ReturnsFalse()
        {
            var pos1 = new GridPosition(2, 3);
            var pos2 = new GridPosition(3, 2);

            Assert.IsFalse(pos1.Equals(pos2));
            Assert.IsTrue(pos1 != pos2);
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var pos = new GridPosition(4, 7);

            Assert.AreEqual("(4, 7)", pos.ToString());
        }
    }
}
