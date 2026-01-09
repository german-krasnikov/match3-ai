// Assets/Tests/EditMode/Features/Board/BoardModelTests.cs
using NUnit.Framework;
using Common;
using Features.Board.Models;

namespace Features.Board.Models
{
    [TestFixture]
    public class BoardModelTests
    {
        private BoardModel _board;
        private const int Width = 8;
        private const int Height = 8;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardModel(Width, Height);
        }

        [Test]
        public void Constructor_CreatesCorrectSize()
        {
            Assert.AreEqual(Width, _board.Width);
            Assert.AreEqual(Height, _board.Height);
        }

        [Test]
        public void GetCell_ValidPosition_ReturnsCell()
        {
            var cell = _board.GetCell(new GridPosition(0, 0));

            Assert.IsNotNull(cell);
        }

        [Test]
        public void GetCell_InvalidPosition_ReturnsNull()
        {
            var cell = _board.GetCell(new GridPosition(-1, 0));

            Assert.IsNull(cell);
        }

        [Test]
        public void SetElement_AddsElementToCell()
        {
            var pos = new GridPosition(3, 3);
            var element = new Element(ElementType.Red);

            _board.SetElement(pos, element);

            Assert.AreEqual(element, _board.GetElement(pos));
        }

        [Test]
        public void SetElement_RaisesOnElementAdded()
        {
            GridPosition receivedPos = GridPosition.Invalid;
            ElementType receivedType = ElementType.None;
            _board.OnElementAdded += (pos, type) =>
            {
                receivedPos = pos;
                receivedType = type;
            };

            var position = new GridPosition(2, 2);
            _board.SetElement(position, new Element(ElementType.Blue));

            Assert.AreEqual(position, receivedPos);
            Assert.AreEqual(ElementType.Blue, receivedType);
        }

        [Test]
        public void RemoveElement_RemovesElementFromCell()
        {
            var pos = new GridPosition(1, 1);
            _board.SetElement(pos, new Element(ElementType.Green));

            _board.RemoveElement(pos);

            Assert.IsNull(_board.GetElement(pos));
        }

        [Test]
        public void RemoveElement_RaisesOnElementRemoved()
        {
            GridPosition receivedPos = GridPosition.Invalid;
            _board.OnElementRemoved += pos => receivedPos = pos;
            var position = new GridPosition(4, 4);
            _board.SetElement(position, new Element(ElementType.Yellow));

            _board.RemoveElement(position);

            Assert.AreEqual(position, receivedPos);
        }

        [Test]
        public void SwapElements_SwapsElementsBetweenCells()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            var elementA = new Element(ElementType.Red);
            var elementB = new Element(ElementType.Blue);
            _board.SetElement(posA, elementA);
            _board.SetElement(posB, elementB);

            _board.SwapElements(posA, posB);

            Assert.AreEqual(elementB, _board.GetElement(posA));
            Assert.AreEqual(elementA, _board.GetElement(posB));
        }

        [Test]
        public void SwapElements_RaisesOnElementsSwapped()
        {
            GridPosition receivedA = GridPosition.Invalid;
            GridPosition receivedB = GridPosition.Invalid;
            _board.OnElementsSwapped += (a, b) =>
            {
                receivedA = a;
                receivedB = b;
            };
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _board.SwapElements(posA, posB);

            Assert.AreEqual(posA, receivedA);
            Assert.AreEqual(posB, receivedB);
        }

        [Test]
        public void IsValidPosition_ValidPosition_ReturnsTrue()
        {
            Assert.IsTrue(_board.IsValidPosition(new GridPosition(0, 0)));
            Assert.IsTrue(_board.IsValidPosition(new GridPosition(Width - 1, Height - 1)));
        }

        [Test]
        public void IsValidPosition_InvalidPosition_ReturnsFalse()
        {
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(-1, 0)));
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(0, -1)));
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(Width, 0)));
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(0, Height)));
        }
    }
}
