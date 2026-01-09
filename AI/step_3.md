# Step 3: Match Detection

## Overview
Реализация сервиса поиска матчей (горизонтальные + вертикальные линии 3+ одинаковых элементов).

## Prerequisites
- Step 1 completed (BoardModel, Cell, Element, GridPosition, ElementType)

---

## 0. Parallel Execution Plan

### Task Dependency Graph
```mermaid
graph LR
    T1[Task 1: IMatchService] --> T2[Task 2: MatchService]
    T2 --> T3[Task 3: MatchServiceTests]
```

### Parallel Groups
| Group | Tasks | Can Run In Parallel |
|-------|-------|---------------------|
| Group A | Task 1 | Independent |
| Group B | Task 2 | After Group A |
| Group C | Task 3 | After Group B |

### Task Assignments
- **Task 1**: IMatchService interface - defines contract
- **Task 2**: MatchService implementation - pure C# service
- **Task 3**: MatchServiceTests - unit tests for all match scenarios

---

## 1. Components

### 1.1 IMatchService
**Type:** Interface
**Responsibility:** Contract for match detection service
**Dependencies:** None (uses BoardModel, GridPosition, ElementType from Step 1)

### 1.2 MatchService
**Type:** Service (Pure C#)
**Responsibility:** Find horizontal and vertical matches of 3+ same elements
**Dependencies:** BoardModel (read-only access)

---

## 2. Interfaces

```csharp
// IMatchService.cs
using System.Collections.Generic;
using Features.Board.Models;
using Common;

namespace Features.Board.Services
{
    public interface IMatchService
    {
        /// <summary>
        /// Finds all matches on the board (horizontal + vertical lines of 3+).
        /// Returns unique positions (no duplicates for intersections).
        /// </summary>
        List<GridPosition> FindAllMatches(BoardModel board);

        /// <summary>
        /// Finds matches that include the specified position.
        /// Used after swap to check if swap created a match.
        /// </summary>
        List<GridPosition> FindMatchesAt(BoardModel board, GridPosition pos);

        /// <summary>
        /// Checks if placing element of given type at position would create a match.
        /// Used by SpawnService to avoid initial matches.
        /// </summary>
        bool WouldCreateMatch(BoardModel board, GridPosition pos, ElementType type);
    }
}
```

---

## 3. Implementation Specs

### 3.1 MatchService.cs

```csharp
// MatchService.cs
using System.Collections.Generic;
using Features.Board.Models;
using Common;

namespace Features.Board.Services
{
    /// <summary>
    /// Stateless service for detecting matches on the board.
    /// Pure C# - testable without Unity.
    /// </summary>
    public class MatchService : IMatchService
    {
        private const int MinMatchLength = 3;

        public List<GridPosition> FindAllMatches(BoardModel board)
        {
            var matches = new HashSet<GridPosition>();

            // Check horizontal matches
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width - MinMatchLength + 1; x++)
                {
                    var line = GetHorizontalLine(board, x, y);
                    if (line.Count >= MinMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);
                        x += line.Count - 1; // Skip matched positions
                    }
                }
            }

            // Check vertical matches
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height - MinMatchLength + 1; y++)
                {
                    var line = GetVerticalLine(board, x, y);
                    if (line.Count >= MinMatchLength)
                    {
                        foreach (var pos in line)
                            matches.Add(pos);
                        y += line.Count - 1; // Skip matched positions
                    }
                }
            }

            return new List<GridPosition>(matches);
        }

        public List<GridPosition> FindMatchesAt(BoardModel board, GridPosition pos)
        {
            var matches = new HashSet<GridPosition>();
            var element = board.GetElement(pos);

            if (element == null || !element.CanMatch)
                return new List<GridPosition>();

            // Check horizontal line through position
            var horizontal = GetHorizontalLineThrough(board, pos);
            if (horizontal.Count >= MinMatchLength)
            {
                foreach (var p in horizontal)
                    matches.Add(p);
            }

            // Check vertical line through position
            var vertical = GetVerticalLineThrough(board, pos);
            if (vertical.Count >= MinMatchLength)
            {
                foreach (var p in vertical)
                    matches.Add(p);
            }

            return new List<GridPosition>(matches);
        }

        public bool WouldCreateMatch(BoardModel board, GridPosition pos, ElementType type)
        {
            if (type == ElementType.None)
                return false;

            // Check horizontal: count same type to left and right
            int horizontalCount = 1;
            horizontalCount += CountInDirection(board, pos, -1, 0, type);
            horizontalCount += CountInDirection(board, pos, 1, 0, type);

            if (horizontalCount >= MinMatchLength)
                return true;

            // Check vertical: count same type up and down
            int verticalCount = 1;
            verticalCount += CountInDirection(board, pos, 0, -1, type);
            verticalCount += CountInDirection(board, pos, 0, 1, type);

            return verticalCount >= MinMatchLength;
        }

        private List<GridPosition> GetHorizontalLine(BoardModel board, int startX, int y)
        {
            var line = new List<GridPosition>();
            var startPos = new GridPosition(startX, y);
            var element = board.GetElement(startPos);

            if (element == null || !element.CanMatch)
                return line;

            line.Add(startPos);

            for (int x = startX + 1; x < board.Width; x++)
            {
                var pos = new GridPosition(x, y);
                var current = board.GetElement(pos);

                if (current != null && element.Matches(current))
                    line.Add(pos);
                else
                    break;
            }

            return line;
        }

        private List<GridPosition> GetVerticalLine(BoardModel board, int x, int startY)
        {
            var line = new List<GridPosition>();
            var startPos = new GridPosition(x, startY);
            var element = board.GetElement(startPos);

            if (element == null || !element.CanMatch)
                return line;

            line.Add(startPos);

            for (int y = startY + 1; y < board.Height; y++)
            {
                var pos = new GridPosition(x, y);
                var current = board.GetElement(pos);

                if (current != null && element.Matches(current))
                    line.Add(pos);
                else
                    break;
            }

            return line;
        }

        private List<GridPosition> GetHorizontalLineThrough(BoardModel board, GridPosition pos)
        {
            var line = new List<GridPosition>();
            var element = board.GetElement(pos);

            if (element == null || !element.CanMatch)
                return line;

            // Find leftmost position of the line
            int left = pos.X;
            while (left > 0)
            {
                var leftPos = new GridPosition(left - 1, pos.Y);
                var leftElement = board.GetElement(leftPos);
                if (leftElement != null && element.Matches(leftElement))
                    left--;
                else
                    break;
            }

            // Collect all positions from left to right
            for (int x = left; x < board.Width; x++)
            {
                var currentPos = new GridPosition(x, pos.Y);
                var current = board.GetElement(currentPos);

                if (current != null && element.Matches(current))
                    line.Add(currentPos);
                else
                    break;
            }

            return line;
        }

        private List<GridPosition> GetVerticalLineThrough(BoardModel board, GridPosition pos)
        {
            var line = new List<GridPosition>();
            var element = board.GetElement(pos);

            if (element == null || !element.CanMatch)
                return line;

            // Find topmost position of the line (lowest Y)
            int top = pos.Y;
            while (top > 0)
            {
                var topPos = new GridPosition(pos.X, top - 1);
                var topElement = board.GetElement(topPos);
                if (topElement != null && element.Matches(topElement))
                    top--;
                else
                    break;
            }

            // Collect all positions from top to bottom
            for (int y = top; y < board.Height; y++)
            {
                var currentPos = new GridPosition(pos.X, y);
                var current = board.GetElement(currentPos);

                if (current != null && element.Matches(current))
                    line.Add(currentPos);
                else
                    break;
            }

            return line;
        }

        private int CountInDirection(BoardModel board, GridPosition start, int dx, int dy, ElementType type)
        {
            int count = 0;
            int x = start.X + dx;
            int y = start.Y + dy;

            while (board.IsValidPosition(new GridPosition(x, y)))
            {
                var element = board.GetElement(new GridPosition(x, y));
                if (element != null && element.Type == type)
                {
                    count++;
                    x += dx;
                    y += dy;
                }
                else
                {
                    break;
                }
            }

            return count;
        }
    }
}
```

---

## 4. Test Specs

### 4.1 MatchServiceTests.cs

```csharp
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
```

---

## 5. Scene Setup Script

Scene Setup не требуется для Step 3. MatchService - это pure C# сервис без визуального представления. Тестирование выполняется через EditMode тесты.

---

## 6. Validation Checklist

- [ ] IMatchService.cs compiles without errors
- [ ] MatchService.cs compiles without errors
- [ ] MatchServiceTests.cs compiles without errors
- [ ] All tests pass (run_tests EditMode)
- [ ] MatchService has no Unity dependencies
- [ ] MatchService is stateless (no internal state between calls)
- [ ] FindAllMatches returns unique positions (no duplicates)
- [ ] FindMatchesAt works for positions in middle and at edges
- [ ] WouldCreateMatch correctly predicts match creation

---

## 7. Files to Create

| File | Path | Type |
|------|------|------|
| IMatchService.cs | Assets/Scripts/Features/Board/Services/ | Interface |
| MatchService.cs | Assets/Scripts/Features/Board/Services/ | Service |
| MatchServiceTests.cs | Assets/Tests/EditMode/Features/Board/ | Test |

---

## 8. Dependencies from Step 1

This step requires the following from Step 1:

```csharp
// Common/GridPosition.cs
public readonly struct GridPosition
{
    public readonly int X;
    public readonly int Y;
    public GridPosition(int x, int y) { X = x; Y = y; }
}

// Common/ElementType.cs
public enum ElementType { None = 0, Red = 1, Blue = 2, Green = 3, Yellow = 4, Purple = 5 }

// Features/Board/Models/Element.cs
public class Element
{
    public ElementType Type { get; }
    public bool CanMatch => Type != ElementType.None;
    public bool Matches(Element other) => other != null && Type == other.Type && CanMatch;
}

// Features/Board/Models/BoardModel.cs
public class BoardModel
{
    public int Width { get; }
    public int Height { get; }
    public Element GetElement(GridPosition pos);
    public void SetElement(GridPosition pos, Element element);
    public bool IsValidPosition(GridPosition pos);
}
```
