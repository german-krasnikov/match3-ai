// Tests/EditMode/Features/Board/BoardModelTests.cs
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

        // Constructor tests
        [Test]
        public void Constructor_CreatesGridWithCorrectDimensions()
        {
            Assert.AreEqual(Width, _board.Width);
            Assert.AreEqual(Height, _board.Height);
        }

        [Test]
        public void Constructor_AllCellsAreEmpty()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = _board.GetCell(new GridPosition(x, y));
                    Assert.IsTrue(cell.IsEmpty);
                }
            }
        }

        // IsValidPosition tests
        [Test]
        public void IsValidPosition_ValidPosition_ReturnsTrue()
        {
            Assert.IsTrue(_board.IsValidPosition(new GridPosition(0, 0)));
            Assert.IsTrue(_board.IsValidPosition(new GridPosition(7, 7)));
            Assert.IsTrue(_board.IsValidPosition(new GridPosition(4, 4)));
        }

        [Test]
        public void IsValidPosition_InvalidPosition_ReturnsFalse()
        {
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(-1, 0)));
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(0, -1)));
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(8, 0)));
            Assert.IsFalse(_board.IsValidPosition(new GridPosition(0, 8)));
        }

        // GetCell tests
        [Test]
        public void GetCell_ValidPosition_ReturnsCell()
        {
            var pos = new GridPosition(3, 4);
            var cell = _board.GetCell(pos);

            Assert.IsNotNull(cell);
            Assert.AreEqual(pos, cell.Position);
        }

        [Test]
        public void GetCell_InvalidPosition_ReturnsNull()
        {
            var cell = _board.GetCell(new GridPosition(-1, 0));
            Assert.IsNull(cell);
        }

        // SetElement tests
        [Test]
        public void SetElement_ValidPosition_SetsElement()
        {
            var pos = new GridPosition(2, 3);
            var element = new Element(ElementType.Red);

            _board.SetElement(pos, element);

            var result = _board.GetElement(pos);
            Assert.AreEqual(ElementType.Red, result.Type);
        }

        [Test]
        public void SetElement_RaisesOnElementAdded()
        {
            var pos = new GridPosition(2, 3);
            var element = new Element(ElementType.Blue);
            GridPosition receivedPos = GridPosition.Invalid;
            ElementType receivedType = ElementType.None;

            _board.OnElementAdded += (p, t) =>
            {
                receivedPos = p;
                receivedType = t;
            };

            _board.SetElement(pos, element);

            Assert.AreEqual(pos, receivedPos);
            Assert.AreEqual(ElementType.Blue, receivedType);
        }

        [Test]
        public void SetElement_InvalidPosition_DoesNothing()
        {
            var pos = new GridPosition(-1, 0);
            var element = new Element(ElementType.Red);
            bool eventRaised = false;

            _board.OnElementAdded += (p, t) => eventRaised = true;

            _board.SetElement(pos, element);

            Assert.IsFalse(eventRaised);
        }

        // RemoveElement tests
        [Test]
        public void RemoveElement_ExistingElement_RemovesIt()
        {
            var pos = new GridPosition(2, 3);
            _board.SetElement(pos, new Element(ElementType.Green));

            _board.RemoveElement(pos);

            Assert.IsNull(_board.GetElement(pos));
        }

        [Test]
        public void RemoveElement_RaisesOnElementRemoved()
        {
            var pos = new GridPosition(2, 3);
            _board.SetElement(pos, new Element(ElementType.Yellow));
            GridPosition receivedPos = GridPosition.Invalid;

            _board.OnElementRemoved += p => receivedPos = p;

            _board.RemoveElement(pos);

            Assert.AreEqual(pos, receivedPos);
        }

        [Test]
        public void RemoveElement_EmptyCell_DoesNotRaiseEvent()
        {
            var pos = new GridPosition(2, 3);
            bool eventRaised = false;

            _board.OnElementRemoved += p => eventRaised = true;

            _board.RemoveElement(pos);

            Assert.IsFalse(eventRaised);
        }

        // SwapElements tests
        [Test]
        public void SwapElements_SwapsCorrectly()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            var elementA = new Element(ElementType.Red);
            var elementB = new Element(ElementType.Blue);

            _board.SetElement(posA, elementA);
            _board.SetElement(posB, elementB);

            _board.SwapElements(posA, posB);

            Assert.AreEqual(ElementType.Blue, _board.GetElement(posA).Type);
            Assert.AreEqual(ElementType.Red, _board.GetElement(posB).Type);
        }

        [Test]
        public void SwapElements_RaisesOnElementsSwapped()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            GridPosition receivedA = GridPosition.Invalid;
            GridPosition receivedB = GridPosition.Invalid;

            _board.OnElementsSwapped += (a, b) =>
            {
                receivedA = a;
                receivedB = b;
            };

            _board.SwapElements(posA, posB);

            Assert.AreEqual(posA, receivedA);
            Assert.AreEqual(posB, receivedB);
        }

        [Test]
        public void SwapElements_WithEmptyCell_SwapsCorrectly()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            // posB is empty

            _board.SwapElements(posA, posB);

            Assert.IsNull(_board.GetElement(posA));
            Assert.AreEqual(ElementType.Red, _board.GetElement(posB).Type);
        }
    }

    [TestFixture]
    public class ElementTests
    {
        [Test]
        public void Constructor_SetsType()
        {
            var element = new Element(ElementType.Red);
            Assert.AreEqual(ElementType.Red, element.Type);
        }

        [Test]
        public void CanMatch_NotNone_ReturnsTrue()
        {
            var element = new Element(ElementType.Blue);
            Assert.IsTrue(element.CanMatch);
        }

        [Test]
        public void CanMatch_None_ReturnsFalse()
        {
            var element = new Element(ElementType.None);
            Assert.IsFalse(element.CanMatch);
        }

        [Test]
        public void Matches_SameType_ReturnsTrue()
        {
            var a = new Element(ElementType.Green);
            var b = new Element(ElementType.Green);
            Assert.IsTrue(a.Matches(b));
        }

        [Test]
        public void Matches_DifferentType_ReturnsFalse()
        {
            var a = new Element(ElementType.Green);
            var b = new Element(ElementType.Yellow);
            Assert.IsFalse(a.Matches(b));
        }

        [Test]
        public void Matches_Null_ReturnsFalse()
        {
            var a = new Element(ElementType.Red);
            Assert.IsFalse(a.Matches(null));
        }

        [Test]
        public void Matches_BothNone_ReturnsFalse()
        {
            var a = new Element(ElementType.None);
            var b = new Element(ElementType.None);
            Assert.IsFalse(a.Matches(b));
        }
    }

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
        public void IsEmpty_NoElement_ReturnsTrue()
        {
            var cell = new Cell(new GridPosition(0, 0));
            Assert.IsTrue(cell.IsEmpty);
        }

        [Test]
        public void IsEmpty_HasElement_ReturnsFalse()
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
        public void RemoveElement_ReturnsAndRemoves()
        {
            var cell = new Cell(new GridPosition(0, 0));
            var element = new Element(ElementType.Purple);
            cell.SetElement(element);

            var removed = cell.RemoveElement();

            Assert.AreEqual(element, removed);
            Assert.IsTrue(cell.IsEmpty);
        }
    }

    [TestFixture]
    public class GridPositionTests
    {
        [Test]
        public void Constructor_SetsCoordinates()
        {
            var pos = new GridPosition(3, 7);
            Assert.AreEqual(3, pos.X);
            Assert.AreEqual(7, pos.Y);
        }

        [Test]
        public void Invalid_ReturnsNegativeCoordinates()
        {
            var invalid = GridPosition.Invalid;
            Assert.AreEqual(-1, invalid.X);
            Assert.AreEqual(-1, invalid.Y);
        }

        [Test]
        public void IsValid_ValidPosition_ReturnsTrue()
        {
            var pos = new GridPosition(0, 0);
            Assert.IsTrue(pos.IsValid);
        }

        [Test]
        public void IsValid_InvalidPosition_ReturnsFalse()
        {
            Assert.IsFalse(GridPosition.Invalid.IsValid);
            Assert.IsFalse(new GridPosition(-1, 0).IsValid);
            Assert.IsFalse(new GridPosition(0, -1).IsValid);
        }

        [Test]
        public void Equals_SameCoordinates_ReturnsTrue()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(2, 3);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
        }

        [Test]
        public void Equals_DifferentCoordinates_ReturnsFalse()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(3, 2);
            Assert.IsFalse(a.Equals(b));
            Assert.IsTrue(a != b);
        }
    }
}
