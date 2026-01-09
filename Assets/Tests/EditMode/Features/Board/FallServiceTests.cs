// Assets/Tests/EditMode/Features/Board/FallServiceTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using Common;
using Features.Board.Models;
using Features.Board.Services;

namespace Features.Board.Services
{
    [TestFixture]
    public class FallServiceTests
    {
        private FallService _fallService;
        private BoardModel _board;

        private const int Width = 8;
        private const int Height = 8;

        [SetUp]
        public void SetUp()
        {
            _fallService = new FallService();
            _board = new BoardModel(Width, Height);
        }

        // === CalculateFalls Tests ===

        [Test]
        public void CalculateFalls_EmptyBoard_ReturnsEmpty()
        {
            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(0, moves.Count);
        }

        [Test]
        public void CalculateFalls_FullColumn_NoFalls()
        {
            // Fill entire column 0
            for (int y = 0; y < Height; y++)
            {
                _board.SetElement(new GridPosition(0, y), new Element(ElementType.Red));
            }

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(0, moves.Count);
        }

        [Test]
        public void CalculateFalls_SingleElementAboveEmpty_Falls()
        {
            // Element at (0, 1), empty at (0, 0)
            _board.SetElement(new GridPosition(0, 1), new Element(ElementType.Red));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(new GridPosition(0, 1), moves[0].From);
            Assert.AreEqual(new GridPosition(0, 0), moves[0].To);
        }

        [Test]
        public void CalculateFalls_MultipleElementsAboveEmpty_AllFall()
        {
            // Elements at (0, 2) and (0, 3), empty at (0, 0) and (0, 1)
            _board.SetElement(new GridPosition(0, 2), new Element(ElementType.Red));
            _board.SetElement(new GridPosition(0, 3), new Element(ElementType.Blue));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(2, moves.Count);

            // Element at (0,2) falls to (0,0)
            Assert.AreEqual(new GridPosition(0, 2), moves[0].From);
            Assert.AreEqual(new GridPosition(0, 0), moves[0].To);

            // Element at (0,3) falls to (0,1)
            Assert.AreEqual(new GridPosition(0, 3), moves[1].From);
            Assert.AreEqual(new GridPosition(0, 1), moves[1].To);
        }

        [Test]
        public void CalculateFalls_GapInMiddle_ElementsFallToFillGap()
        {
            // Column: [Red at 0, empty at 1, Blue at 2]
            _board.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _board.SetElement(new GridPosition(0, 2), new Element(ElementType.Blue));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(1, moves.Count);
            Assert.AreEqual(new GridPosition(0, 2), moves[0].From);
            Assert.AreEqual(new GridPosition(0, 1), moves[0].To);
        }

        [Test]
        public void CalculateFalls_MultipleGaps_ElementsFallCorrectly()
        {
            // Column: [empty, empty, Red, empty, Blue]
            // y=0: empty, y=1: empty, y=2: Red, y=3: empty, y=4: Blue
            _board.SetElement(new GridPosition(0, 2), new Element(ElementType.Red));
            _board.SetElement(new GridPosition(0, 4), new Element(ElementType.Blue));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(2, moves.Count);

            // Red at (0,2) falls to (0,0)
            Assert.AreEqual(new GridPosition(0, 2), moves[0].From);
            Assert.AreEqual(new GridPosition(0, 0), moves[0].To);

            // Blue at (0,4) falls to (0,1)
            Assert.AreEqual(new GridPosition(0, 4), moves[1].From);
            Assert.AreEqual(new GridPosition(0, 1), moves[1].To);
        }

        [Test]
        public void CalculateFalls_MultipleColumns_ProcessesAllColumns()
        {
            // Column 0: element at y=1, empty at y=0
            // Column 1: element at y=2, empty at y=0,1
            _board.SetElement(new GridPosition(0, 1), new Element(ElementType.Red));
            _board.SetElement(new GridPosition(1, 2), new Element(ElementType.Blue));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(2, moves.Count);
        }

        [Test]
        public void CalculateFalls_ElementAtBottom_NoFall()
        {
            // Element at bottom position
            _board.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(0, moves.Count);
        }

        [Test]
        public void CalculateFalls_ConsecutiveElements_AllFallCorrectly()
        {
            // y=3: Red, y=4: Blue, y=5: Green, empty below
            _board.SetElement(new GridPosition(0, 3), new Element(ElementType.Red));
            _board.SetElement(new GridPosition(0, 4), new Element(ElementType.Blue));
            _board.SetElement(new GridPosition(0, 5), new Element(ElementType.Green));

            var moves = _fallService.CalculateFalls(_board);

            Assert.AreEqual(3, moves.Count);

            // Red at (0,3) falls to (0,0)
            Assert.AreEqual(new GridPosition(0, 3), moves[0].From);
            Assert.AreEqual(new GridPosition(0, 0), moves[0].To);

            // Blue at (0,4) falls to (0,1)
            Assert.AreEqual(new GridPosition(0, 4), moves[1].From);
            Assert.AreEqual(new GridPosition(0, 1), moves[1].To);

            // Green at (0,5) falls to (0,2)
            Assert.AreEqual(new GridPosition(0, 5), moves[2].From);
            Assert.AreEqual(new GridPosition(0, 2), moves[2].To);
        }

        // === GetEmptyTopPositions Tests ===

        [Test]
        public void GetEmptyTopPositions_FullBoard_ReturnsEmpty()
        {
            // Fill entire board
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _board.SetElement(new GridPosition(x, y), new Element(ElementType.Red));
                }
            }

            var emptyPositions = _fallService.GetEmptyTopPositions(_board);

            Assert.AreEqual(0, emptyPositions.Count);
        }

        [Test]
        public void GetEmptyTopPositions_EmptyBoard_ReturnsAllPositions()
        {
            var emptyPositions = _fallService.GetEmptyTopPositions(_board);

            Assert.AreEqual(Width * Height, emptyPositions.Count);
        }

        [Test]
        public void GetEmptyTopPositions_OneElementPerColumn_ReturnsTopPositions()
        {
            // One element at bottom of each column
            for (int x = 0; x < Width; x++)
            {
                _board.SetElement(new GridPosition(x, 0), new Element(ElementType.Red));
            }

            var emptyPositions = _fallService.GetEmptyTopPositions(_board);

            // Should return (Height - 1) positions per column
            Assert.AreEqual(Width * (Height - 1), emptyPositions.Count);
        }

        [Test]
        public void GetEmptyTopPositions_SingleColumnEmpty_ReturnsColumnPositions()
        {
            // Fill all columns except column 0
            for (int x = 1; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _board.SetElement(new GridPosition(x, y), new Element(ElementType.Red));
                }
            }

            var emptyPositions = _fallService.GetEmptyTopPositions(_board);

            Assert.AreEqual(Height, emptyPositions.Count);

            // All positions should be in column 0
            foreach (var pos in emptyPositions)
            {
                Assert.AreEqual(0, pos.X);
            }
        }

        [Test]
        public void GetEmptyTopPositions_AfterDestroy_ReturnsCorrectPositions()
        {
            // Simulate destroyed middle section of column 0
            // Elements at y=0 and y=5,6,7 (destroyed y=1,2,3,4)
            _board.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _board.SetElement(new GridPosition(0, 5), new Element(ElementType.Blue));
            _board.SetElement(new GridPosition(0, 6), new Element(ElementType.Green));
            _board.SetElement(new GridPosition(0, 7), new Element(ElementType.Yellow));

            // Fill other columns
            for (int x = 1; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _board.SetElement(new GridPosition(x, y), new Element(ElementType.Red));
                }
            }

            var emptyPositions = _fallService.GetEmptyTopPositions(_board);

            // Column 0 has 4 empties (y=1,2,3,4)
            Assert.AreEqual(4, emptyPositions.Count);
        }
    }
}
