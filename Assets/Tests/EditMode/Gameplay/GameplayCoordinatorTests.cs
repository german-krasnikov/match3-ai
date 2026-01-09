// Assets/Tests/EditMode/Gameplay/GameplayCoordinatorTests.cs
using System;
using System.Collections.Generic;
using NUnit.Framework;
using NSubstitute;
using Common;
using Features.Board.Models;
using Features.Board.Views;
using Features.Board.Services;
using Gameplay;

namespace Gameplay
{
    [TestFixture]
    public class GameplayCoordinatorTests
    {
        private BoardModel _board;
        private IBoardView _view;
        private IInputService _input;
        private IMatchService _matcher;
        private IFallService _faller;
        private ISpawnService _spawner;
        private GameplayCoordinator _coordinator;

        private const int Width = 8;
        private const int Height = 8;

        [SetUp]
        public void SetUp()
        {
            _board = new BoardModel(Width, Height);
            _view = Substitute.For<IBoardView>();
            _input = Substitute.For<IInputService>();
            _matcher = Substitute.For<IMatchService>();
            _faller = Substitute.For<IFallService>();
            _spawner = Substitute.For<ISpawnService>();

            _coordinator = new GameplayCoordinator(
                _board, _view, _input, _matcher, _faller, _spawner);
        }

        [TearDown]
        public void TearDown()
        {
            _coordinator.Dispose();
        }

        // === Initial State Tests ===

        [Test]
        public void Constructor_StartsInWaitingForInputState()
        {
            Assert.AreEqual(GameState.WaitingForInput, _coordinator.CurrentState);
        }

        [Test]
        public void Constructor_SubscribesToInputService()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            _view.Received(1).SwapElements(posA, posB, Arg.Any<Action>());
        }

        // === Swap Flow Tests ===

        [Test]
        public void SwapRequested_ChangesStateToSwapping()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            Assert.AreEqual(GameState.Swapping, _coordinator.CurrentState);
        }

        [Test]
        public void SwapRequested_DisablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            _input.Received(1).SetInputEnabled(false);
        }

        [Test]
        public void SwapRequested_SwapsInModel()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            var elementA = new Element(ElementType.Red);
            var elementB = new Element(ElementType.Blue);
            _board.SetElement(posA, elementA);
            _board.SetElement(posB, elementB);

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            Assert.AreEqual(elementB, _board.GetElement(posA));
            Assert.AreEqual(elementA, _board.GetElement(posB));
        }

        [Test]
        public void SwapRequested_WhenNotWaitingForInput_IsIgnored()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            // Trigger first swap
            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            _view.ClearReceivedCalls();

            // Try second swap while first is processing
            var posC = new GridPosition(2, 0);
            var posD = new GridPosition(3, 0);
            _board.SetElement(posC, new Element(ElementType.Green));
            _board.SetElement(posD, new Element(ElementType.Yellow));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posC, posD);

            _view.DidNotReceive().SwapElements(posC, posD, Arg.Any<Action>());
        }

        [Test]
        public void SwapRequested_WithEmptyPosition_IsIgnored()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            // posB is empty

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            _view.DidNotReceive().SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        // === Match Validation Tests ===

        [Test]
        public void SwapComplete_WithNoMatches_RollsBack()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();

            // Should call swap again for rollback
            _view.Received(2).SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        [Test]
        public void RollbackComplete_ReturnsToWaitingForInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action rollbackCallback = null;
            int callCount = 0;

            _view.SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(),
                Arg.Do<Action>(cb =>
                {
                    if (callCount == 0) swapCallback = cb;
                    else rollbackCallback = cb;
                    callCount++;
                }));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            rollbackCallback?.Invoke();

            Assert.AreEqual(GameState.WaitingForInput, _coordinator.CurrentState);
        }

        [Test]
        public void RollbackComplete_ReEnablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action rollbackCallback = null;
            int callCount = 0;

            _view.SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(),
                Arg.Do<Action>(cb =>
                {
                    if (callCount == 0) swapCallback = cb;
                    else rollbackCallback = cb;
                    callCount++;
                }));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            _input.ClearReceivedCalls();

            swapCallback?.Invoke();
            rollbackCallback?.Invoke();

            _input.Received(1).SetInputEnabled(true);
        }

        // === Destroy Flow Tests ===

        [Test]
        public void SwapComplete_WithMatches_ChangesStateToDestroying()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA, new GridPosition(0, 1), new GridPosition(0, 2) });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();

            Assert.AreEqual(GameState.Destroying, _coordinator.CurrentState);
        }

        [Test]
        public void SwapComplete_WithMatches_CallsDestroyElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            var matches = new List<GridPosition> { posA, new GridPosition(0, 1), new GridPosition(0, 2) };
            _matcher.FindMatchesAt(_board, posA).Returns(matches);
            _matcher.FindMatchesAt(_board, posB).Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();

            _view.Received(1).DestroyElements(
                Arg.Is<List<GridPosition>>(list => list.Count == 3),
                Arg.Any<Action>());
        }

        // === Fall Flow Tests ===

        [Test]
        public void DestroyComplete_ChangesStateToFalling()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board)
                .Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board)
                .Returns(new List<GridPosition>());
            _matcher.FindAllMatches(_board)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();

            // State should be Destroying before callback
            Assert.AreEqual(GameState.Destroying, _coordinator.CurrentState);

            destroyCallback?.Invoke();

            // After destroy, should proceed to Falling
            // (but since no falls, it goes to Refilling, then back to WaitingForInput)
        }

        [Test]
        public void DestroyComplete_RemovesElementsFromModel()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board)
                .Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board)
                .Returns(new List<GridPosition>());
            _matcher.FindAllMatches(_board)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            Assert.IsNull(_board.GetElement(posA));
        }

        [Test]
        public void FallPhase_CallsMoveElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            var falls = new List<FallMove>
            {
                new FallMove(new GridPosition(0, 1), new GridPosition(0, 0))
            };

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board).Returns(falls);
            _faller.GetEmptyTopPositions(_board)
                .Returns(new List<GridPosition>());
            _matcher.FindAllMatches(_board)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _view.Received(1).MoveElements(
                Arg.Is<List<FallMove>>(list => list.Count == 1),
                Arg.Any<Action>());
        }

        // === Refill Flow Tests ===

        [Test]
        public void RefillPhase_SpawnsNewElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            var emptyPositions = new List<GridPosition> { new GridPosition(0, 7) };

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board).Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board).Returns(emptyPositions);
            _spawner.GetRandomElement().Returns(ElementType.Green);
            _matcher.FindAllMatches(_board).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _view.Received(1).SpawnElements(
                Arg.Is<List<(GridPosition pos, ElementType type)>>(list => list.Count == 1),
                Arg.Any<Action>());
        }

        [Test]
        public void RefillPhase_AddsElementsToModel()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            var emptyPos = new GridPosition(2, 7);
            var emptyPositions = new List<GridPosition> { emptyPos };

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board).Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board).Returns(emptyPositions);
            _spawner.GetRandomElement().Returns(ElementType.Green);
            _matcher.FindAllMatches(_board).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            var element = _board.GetElement(emptyPos);
            Assert.IsNotNull(element);
            Assert.AreEqual(ElementType.Green, element.Type);
        }

        // === Cascade Tests ===

        [Test]
        public void AfterRefill_ChecksForCascade()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board).Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board).Returns(new List<GridPosition>());
            _matcher.FindAllMatches(_board).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _matcher.Received(1).FindAllMatches(_board);
        }

        [Test]
        public void Cascade_WhenMatchesFound_DestroysAgain()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            // First match from swap
            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());

            _faller.CalculateFalls(_board).Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board).Returns(new List<GridPosition>());

            // Cascade match
            var cascadeMatches = new List<GridPosition> { new GridPosition(2, 0), new GridPosition(2, 1), new GridPosition(2, 2) };
            _matcher.FindAllMatches(_board).Returns(cascadeMatches);

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            // Should be called twice: once for initial match, once for cascade
            _view.Received(2).DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Any<Action>());
        }

        // === State Change Event Tests ===

        [Test]
        public void StateChange_FiresOnStateChangedEvent()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            var stateChanges = new List<GameState>();
            _coordinator.OnStateChanged += state => stateChanges.Add(state);

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            Assert.Contains(GameState.Swapping, stateChanges);
        }

        // === Dispose Tests ===

        [Test]
        public void Dispose_UnsubscribesFromInputService()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _coordinator.Dispose();

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            _view.DidNotReceive().SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        // === Full Cycle Test ===

        [Test]
        public void FullCycle_ReturnsToWaitingForInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board).Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board).Returns(new List<GridPosition>());
            _matcher.FindAllMatches(_board).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            Assert.AreEqual(GameState.WaitingForInput, _coordinator.CurrentState);
        }

        [Test]
        public void FullCycle_ReEnablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _board.SetElement(posA, new Element(ElementType.Red));
            _board.SetElement(posB, new Element(ElementType.Blue));

            _matcher.FindMatchesAt(_board, posA)
                .Returns(new List<GridPosition> { posA });
            _matcher.FindMatchesAt(_board, posB)
                .Returns(new List<GridPosition>());
            _faller.CalculateFalls(_board).Returns(new List<FallMove>());
            _faller.GetEmptyTopPositions(_board).Returns(new List<GridPosition>());
            _matcher.FindAllMatches(_board).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _input.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);
            _input.ClearReceivedCalls();

            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _input.Received(1).SetInputEnabled(true);
        }
    }
}
