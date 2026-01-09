// Tests/EditMode/Features/Board/MatchServiceTests.cs
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Features.Board.Models;
using Features.Board.Services;
using Common;

namespace Features.Board.Services
{
    [TestFixture]
    public class MatchServiceTests
    {
        private MatchService _service;
        private BoardModel _board;

        [SetUp]
        public void SetUp()
        {
            _service = new MatchService();
            _board = new BoardModel(8, 8);
        }

        #region FindAllMatches - Horizontal

        [Test]
        public void FindAllMatches_HorizontalLine3_ReturnsThreePositions()
        {
            // Arrange: R R R at row 0
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);
            SetElement(2, 0, ElementType.Red);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(3, matches.Count);
            Assert.IsTrue(ContainsPosition(matches, 0, 0));
            Assert.IsTrue(ContainsPosition(matches, 1, 0));
            Assert.IsTrue(ContainsPosition(matches, 2, 0));
        }

        [Test]
        public void FindAllMatches_HorizontalLine4_ReturnsFourPositions()
        {
            // Arrange: B B B B at row 2
            SetElement(0, 2, ElementType.Blue);
            SetElement(1, 2, ElementType.Blue);
            SetElement(2, 2, ElementType.Blue);
            SetElement(3, 2, ElementType.Blue);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(4, matches.Count);
        }

        [Test]
        public void FindAllMatches_HorizontalLine5_ReturnsFivePositions()
        {
            // Arrange: G G G G G at row 3
            for (int x = 0; x < 5; x++)
                SetElement(x, 3, ElementType.Green);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(5, matches.Count);
        }

        [Test]
        public void FindAllMatches_HorizontalLine2_ReturnsEmpty()
        {
            // Arrange: R R (only 2)
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.IsEmpty(matches);
        }

        #endregion

        #region FindAllMatches - Vertical

        [Test]
        public void FindAllMatches_VerticalLine3_ReturnsThreePositions()
        {
            // Arrange: column of Y Y Y
            SetElement(0, 0, ElementType.Yellow);
            SetElement(0, 1, ElementType.Yellow);
            SetElement(0, 2, ElementType.Yellow);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(3, matches.Count);
            Assert.IsTrue(ContainsPosition(matches, 0, 0));
            Assert.IsTrue(ContainsPosition(matches, 0, 1));
            Assert.IsTrue(ContainsPosition(matches, 0, 2));
        }

        [Test]
        public void FindAllMatches_VerticalLine4_ReturnsFourPositions()
        {
            // Arrange
            for (int y = 0; y < 4; y++)
                SetElement(5, y, ElementType.Purple);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(4, matches.Count);
        }

        [Test]
        public void FindAllMatches_VerticalLine5_ReturnsFivePositions()
        {
            // Arrange
            for (int y = 0; y < 5; y++)
                SetElement(7, y, ElementType.Red);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(5, matches.Count);
        }

        #endregion

        #region FindAllMatches - Intersections

        [Test]
        public void FindAllMatches_CrossIntersection_ReturnsUniquePositions()
        {
            // Arrange: T-shape or cross
            //     R
            //   R R R
            //     R
            SetElement(2, 0, ElementType.Red);
            SetElement(0, 1, ElementType.Red);
            SetElement(1, 1, ElementType.Red);
            SetElement(2, 1, ElementType.Red);
            SetElement(2, 2, ElementType.Red);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert: 5 unique positions (center is shared)
            Assert.AreEqual(5, matches.Count);
        }

        [Test]
        public void FindAllMatches_LShape_ReturnsSixPositions()
        {
            // Arrange: L-shape
            // R R R
            // R
            // R
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);
            SetElement(2, 0, ElementType.Red);
            SetElement(0, 1, ElementType.Red);
            SetElement(0, 2, ElementType.Red);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert: 5 unique (corner shared)
            Assert.AreEqual(5, matches.Count);
        }

        #endregion

        #region FindAllMatches - Multiple Matches

        [Test]
        public void FindAllMatches_TwoSeparateMatches_ReturnsBoth()
        {
            // Arrange: horizontal at row 0, vertical at column 7
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);
            SetElement(2, 0, ElementType.Red);

            SetElement(7, 3, ElementType.Blue);
            SetElement(7, 4, ElementType.Blue);
            SetElement(7, 5, ElementType.Blue);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(6, matches.Count);
        }

        #endregion

        #region FindAllMatches - Edge Cases

        [Test]
        public void FindAllMatches_EmptyBoard_ReturnsEmpty()
        {
            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.IsEmpty(matches);
        }

        [Test]
        public void FindAllMatches_NoMatches_ReturnsEmpty()
        {
            // Arrange: checkerboard pattern
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Blue);
            SetElement(0, 1, ElementType.Blue);
            SetElement(1, 1, ElementType.Red);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.IsEmpty(matches);
        }

        [Test]
        public void FindAllMatches_MatchAtBoardEdge_Works()
        {
            // Arrange: match at right edge
            SetElement(5, 7, ElementType.Green);
            SetElement(6, 7, ElementType.Green);
            SetElement(7, 7, ElementType.Green);

            // Act
            var matches = _service.FindAllMatches(_board);

            // Assert
            Assert.AreEqual(3, matches.Count);
        }

        #endregion

        #region FindMatchesAt

        [Test]
        public void FindMatchesAt_PositionInHorizontalMatch_ReturnsMatch()
        {
            // Arrange
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);
            SetElement(2, 0, ElementType.Red);

            // Act - check middle position
            var matches = _service.FindMatchesAt(_board, new GridPosition(1, 0));

            // Assert
            Assert.AreEqual(3, matches.Count);
        }

        [Test]
        public void FindMatchesAt_PositionInVerticalMatch_ReturnsMatch()
        {
            // Arrange
            SetElement(0, 0, ElementType.Blue);
            SetElement(0, 1, ElementType.Blue);
            SetElement(0, 2, ElementType.Blue);

            // Act
            var matches = _service.FindMatchesAt(_board, new GridPosition(0, 1));

            // Assert
            Assert.AreEqual(3, matches.Count);
        }

        [Test]
        public void FindMatchesAt_PositionInBothMatches_ReturnsCombined()
        {
            // Arrange: cross pattern
            SetElement(1, 0, ElementType.Red);
            SetElement(0, 1, ElementType.Red);
            SetElement(1, 1, ElementType.Red);
            SetElement(2, 1, ElementType.Red);
            SetElement(1, 2, ElementType.Red);

            // Act - check center
            var matches = _service.FindMatchesAt(_board, new GridPosition(1, 1));

            // Assert: 5 unique positions
            Assert.AreEqual(5, matches.Count);
        }

        [Test]
        public void FindMatchesAt_PositionNoMatch_ReturnsEmpty()
        {
            // Arrange
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Blue);

            // Act
            var matches = _service.FindMatchesAt(_board, new GridPosition(0, 0));

            // Assert
            Assert.IsEmpty(matches);
        }

        [Test]
        public void FindMatchesAt_EmptyPosition_ReturnsEmpty()
        {
            // Act
            var matches = _service.FindMatchesAt(_board, new GridPosition(0, 0));

            // Assert
            Assert.IsEmpty(matches);
        }

        #endregion

        #region WouldCreateMatch

        [Test]
        public void WouldCreateMatch_TwoSameToLeft_ReturnsTrue()
        {
            // Arrange: R R _ (checking position 2)
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(2, 0), ElementType.Red);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WouldCreateMatch_TwoSameToRight_ReturnsTrue()
        {
            // Arrange: _ R R (checking position 0)
            SetElement(1, 0, ElementType.Red);
            SetElement(2, 0, ElementType.Red);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(0, 0), ElementType.Red);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WouldCreateMatch_OneSameEachSide_ReturnsTrue()
        {
            // Arrange: R _ R (checking position 1)
            SetElement(0, 0, ElementType.Red);
            SetElement(2, 0, ElementType.Red);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(1, 0), ElementType.Red);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WouldCreateMatch_TwoSameAbove_ReturnsTrue()
        {
            // Arrange: vertical
            SetElement(0, 0, ElementType.Blue);
            SetElement(0, 1, ElementType.Blue);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(0, 2), ElementType.Blue);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WouldCreateMatch_TwoSameBelow_ReturnsTrue()
        {
            // Arrange
            SetElement(0, 1, ElementType.Blue);
            SetElement(0, 2, ElementType.Blue);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(0, 0), ElementType.Blue);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void WouldCreateMatch_OnlyOneSame_ReturnsFalse()
        {
            // Arrange: R _ (only one)
            SetElement(0, 0, ElementType.Red);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(1, 0), ElementType.Red);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void WouldCreateMatch_DifferentTypes_ReturnsFalse()
        {
            // Arrange: R R _ (but checking with Blue)
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(2, 0), ElementType.Blue);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void WouldCreateMatch_NoneType_ReturnsFalse()
        {
            // Arrange
            SetElement(0, 0, ElementType.Red);
            SetElement(1, 0, ElementType.Red);

            // Act
            bool result = _service.WouldCreateMatch(_board, new GridPosition(2, 0), ElementType.None);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion

        #region Helper Methods

        private void SetElement(int x, int y, ElementType type)
        {
            _board.SetElement(new GridPosition(x, y), new Element(type));
        }

        private bool ContainsPosition(List<GridPosition> positions, int x, int y)
        {
            return positions.Any(p => p.X == x && p.Y == y);
        }

        #endregion
    }
}
