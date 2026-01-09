// Assets/Tests/EditMode/Features/Board/CellTests.cs
using NUnit.Framework;
using Common;
using Features.Board.Models;

namespace Features.Board.Models
{
    [TestFixture]
    public class CellTests
    {
        [Test]
        public void Constructor_SetsPosition()
        {
            var pos = new GridPosition(3, 5);

            var cell = new Cell(pos);

            Assert.AreEqual(pos, cell.Position);
        }

        [Test]
        public void IsEmpty_WhenNoElement_ReturnsTrue()
        {
            var cell = new Cell(new GridPosition(0, 0));

            Assert.IsTrue(cell.IsEmpty);
        }

        [Test]
        public void IsEmpty_WhenHasElement_ReturnsFalse()
        {
            var cell = new Cell(new GridPosition(0, 0));
            cell.SetElement(new Element(ElementType.Red));

            Assert.IsFalse(cell.IsEmpty);
        }

        [Test]
        public void SetElement_SetsElement()
        {
            var cell = new Cell(new GridPosition(0, 0));
            var element = new Element(ElementType.Blue);

            cell.SetElement(element);

            Assert.AreEqual(element, cell.Element);
        }

        [Test]
        public void RemoveElement_ReturnsAndRemovesElement()
        {
            var cell = new Cell(new GridPosition(0, 0));
            var element = new Element(ElementType.Green);
            cell.SetElement(element);

            var removed = cell.RemoveElement();

            Assert.AreEqual(element, removed);
            Assert.IsTrue(cell.IsEmpty);
        }

        [Test]
        public void RemoveElement_WhenEmpty_ReturnsNull()
        {
            var cell = new Cell(new GridPosition(0, 0));

            var removed = cell.RemoveElement();

            Assert.IsNull(removed);
        }
    }
}
