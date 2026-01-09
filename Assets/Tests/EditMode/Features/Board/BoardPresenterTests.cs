// Assets/Tests/EditMode/Features/Board/BoardPresenterTests.cs
using System;
using System.Collections.Generic;
using NUnit.Framework;
using NSubstitute;
using Common;
using Features.Board.Models;
using Features.Board.Views;
using Features.Board.Services;
using Features.Board.Presenters;

namespace Features.Board.Presenters
{
    [TestFixture]
    public class BoardPresenterTests
    {
        private BoardModel _model;
        private IBoardView _view;
        private IMatchService _matchService;
        private IInputService _inputService;
        private IFallService _fallService;
        private ISpawnService _spawnService;
        private BoardPresenter _presenter;

        private const int Width = 8;
        private const int Height = 8;
        private const float CellSize = 1f;

        [SetUp]
        public void SetUp()
        {
            _model = new BoardModel(Width, Height);
            _view = Substitute.For<IBoardView>();
            _matchService = Substitute.For<IMatchService>();
            _inputService = Substitute.For<IInputService>();
            _fallService = Substitute.For<IFallService>();
            _spawnService = Substitute.For<ISpawnService>();

            // Defaults
            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>()).Returns(new List<GridPosition>());
            _matchService.FindAllMatches(_model).Returns(new List<GridPosition>());
            _fallService.CalculateFalls(_model).Returns(new List<FallMove>());
            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition>());
            _spawnService.GetRandomElement().Returns(ElementType.Red);

            _presenter = new BoardPresenter(_model, _view, CellSize, _matchService, _inputService, _fallService, _spawnService);
        }

        [TearDown]
        public void TearDown() => _presenter.Dispose();

        // === Constructor Tests ===

        [Test]
        public void Constructor_InitializesView() => _view.Received(1).Initialize(Width, Height, CellSize);

        [Test]
        public void Constructor_SubscribesToModelEvents()
        {
            var pos = new GridPosition(0, 0);
            _model.SetElement(pos, new Element(ElementType.Red));
            _view.Received(1).CreateElement(pos, ElementType.Red);
        }

        // === TrySwap Tests ===

        [Test]
        public void TrySwap_WithValidPositions_SwapsInModel()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            var elementA = new Element(ElementType.Red);
            var elementB = new Element(ElementType.Blue);
            _model.SetElement(posA, elementA);
            _model.SetElement(posB, elementB);

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>()).Returns(new List<GridPosition> { posA });

            _presenter.TrySwap(posA, posB);

            Assert.AreEqual(elementB, _model.GetElement(posA));
            Assert.AreEqual(elementA, _model.GetElement(posB));
        }

        [Test]
        public void TrySwap_CallsViewSwapElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>()).Returns(new List<GridPosition> { posA });

            _presenter.TrySwap(posA, posB);

            _view.Received(1).SwapElements(posA, posB, Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_WithEmptyPosition_DoesNotSwap()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));

            _presenter.TrySwap(posA, posB);

            _view.DidNotReceive().SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_WhenAlreadySwapping_DoesNothing()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));
            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>()).Returns(new List<GridPosition> { posA });

            _presenter.TrySwap(posA, posB);
            _view.ClearReceivedCalls();

            var posC = new GridPosition(2, 0);
            var posD = new GridPosition(3, 0);
            _model.SetElement(posC, new Element(ElementType.Green));
            _model.SetElement(posD, new Element(ElementType.Yellow));

            _presenter.TrySwap(posC, posD);

            _view.DidNotReceive().SwapElements(posC, posD, Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_SetsIsProcessingTrue()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _presenter.TrySwap(posA, posB);

            Assert.IsTrue(_presenter.IsProcessing);
        }

        [Test]
        public void TrySwap_DisablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _presenter.TrySwap(posA, posB);

            _inputService.Received(1).SetInputEnabled(false);
        }

        // === Match Validation Tests ===

        [Test]
        public void TrySwap_NoMatchCreated_RollsBackSwap()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>()).Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            _view.Received(2).SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_NoMatchCreated_RestoresModelState()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            var elementA = new Element(ElementType.Red);
            var elementB = new Element(ElementType.Blue);
            _model.SetElement(posA, elementA);
            _model.SetElement(posB, elementB);

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>()).Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            Assert.AreEqual(elementA, _model.GetElement(posA));
            Assert.AreEqual(elementB, _model.GetElement(posB));
        }

        // === Destroy Tests ===

        [Test]
        public void DestroyMatches_CallsViewDestroyElements()
        {
            var matches = new List<GridPosition> { new(0, 0), new(1, 0), new(2, 0) };
            _presenter.DestroyMatches(matches);
            _view.Received(1).DestroyElements(Arg.Is<List<GridPosition>>(list => list.Count == 3), Arg.Any<Action>());
        }

        [Test]
        public void DestroyMatches_AfterAnimation_RemovesFromModel()
        {
            var pos = new GridPosition(0, 0);
            _model.SetElement(pos, new Element(ElementType.Red));

            Action destroyCallback = null;
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.DestroyMatches(new List<GridPosition> { pos });
            destroyCallback?.Invoke();

            Assert.IsNull(_model.GetElement(pos));
        }

        [Test]
        public void DestroyMatches_FiresOnMatchesDestroyedEvent()
        {
            var pos = new GridPosition(0, 0);
            _model.SetElement(pos, new Element(ElementType.Red));

            bool eventFired = false;
            _presenter.OnMatchesDestroyed += () => eventFired = true;

            Action destroyCallback = null;
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.DestroyMatches(new List<GridPosition> { pos });
            destroyCallback?.Invoke();

            Assert.IsTrue(eventFired);
        }

        // === Fall Tests ===

        [Test]
        public void AfterDestroy_CallsFallServiceCalculateFalls()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            Action swapCallback = null;
            Action destroyCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _fallService.Received(1).CalculateFalls(_model);
        }

        [Test]
        public void WhenFallsExist_CallsViewMoveElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            var fallMoves = new List<FallMove> { new(new GridPosition(0, 2), new GridPosition(0, 0)) };
            _fallService.CalculateFalls(_model).Returns(fallMoves);

            Action swapCallback = null;
            Action destroyCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _view.Received(1).MoveElements(Arg.Is<List<FallMove>>(list => list.Count == 1), Arg.Any<Action>());
        }

        [Test]
        public void AfterFallComplete_FiresOnFallCompleteEvent()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            var fallMoves = new List<FallMove> { new(new GridPosition(0, 2), new GridPosition(0, 0)) };
            _fallService.CalculateFalls(_model).Returns(fallMoves);

            bool eventFired = false;
            _presenter.OnFallComplete += () => eventFired = true;

            Action swapCallback = null;
            Action destroyCallback = null;
            Action fallCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.MoveElements(Arg.Any<List<FallMove>>(), Arg.Do<Action>(cb => fallCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            fallCallback?.Invoke();

            Assert.IsTrue(eventFired);
        }

        // === Refill Tests ===

        [Test]
        public void AfterFall_CallsGetEmptyTopPositions()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            var fallMoves = new List<FallMove> { new(new GridPosition(0, 2), new GridPosition(0, 0)) };
            _fallService.CalculateFalls(_model).Returns(fallMoves);

            Action swapCallback = null;
            Action destroyCallback = null;
            Action fallCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.MoveElements(Arg.Any<List<FallMove>>(), Arg.Do<Action>(cb => fallCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            fallCallback?.Invoke();

            _fallService.Received().GetEmptyTopPositions(_model);
        }

        [Test]
        public void WhenEmptyPositions_CallsSpawnService()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });

            Action swapCallback = null;
            Action destroyCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _spawnService.Received(1).GetRandomElement();
        }

        [Test]
        public void WhenEmptyPositions_CallsViewSpawnElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });

            Action swapCallback = null;
            Action destroyCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _view.Received(1).SpawnElements(Arg.Is<List<SpawnData>>(list => list.Count == 1), Arg.Any<Action>());
        }

        [Test]
        public void AfterSpawn_FiresOnRefillComplete()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });

            bool eventFired = false;
            _presenter.OnRefillComplete += () => eventFired = true;

            Action swapCallback = null;
            Action destroyCallback = null;
            Action spawnCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.SpawnElements(Arg.Any<List<SpawnData>>(), Arg.Do<Action>(cb => spawnCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            spawnCallback?.Invoke();

            Assert.IsTrue(eventFired);
        }

        // === Cascade Tests ===

        [Test]
        public void AfterRefill_ChecksForNewMatches()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });

            Action swapCallback = null;
            Action destroyCallback = null;
            Action spawnCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.SpawnElements(Arg.Any<List<SpawnData>>(), Arg.Do<Action>(cb => spawnCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            _matchService.ClearReceivedCalls();
            spawnCallback?.Invoke();

            _matchService.Received(1).FindAllMatches(_model);
        }

        [Test]
        public void WhenCascadeMatchFound_DestroysAgain()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });

            int findAllMatchesCalls = 0;
            _matchService.FindAllMatches(_model).Returns(x =>
            {
                findAllMatchesCalls++;
                if (findAllMatchesCalls == 1)
                    return new List<GridPosition> { new(2, 0), new(2, 1), new(2, 2) };
                return new List<GridPosition>();
            });

            Action swapCallback = null;
            Action destroyCallback = null;
            Action spawnCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            int destroyCount = 0;
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb =>
            {
                destroyCount++;
                destroyCallback = cb;
            }));

            _view.SpawnElements(Arg.Any<List<SpawnData>>(), Arg.Do<Action>(cb => spawnCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke(); // First destroy
            spawnCallback?.Invoke(); // Triggers cascade check
            destroyCallback?.Invoke(); // Second destroy (cascade)

            _view.Received(2).DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Any<Action>());
        }

        [Test]
        public void WhenNoCascadeMatch_FiresOnCascadeComplete()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });
            _matchService.FindAllMatches(_model).Returns(new List<GridPosition>());

            bool eventFired = false;
            _presenter.OnCascadeComplete += () => eventFired = true;

            Action swapCallback = null;
            Action destroyCallback = null;
            Action spawnCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.SpawnElements(Arg.Any<List<SpawnData>>(), Arg.Do<Action>(cb => spawnCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            spawnCallback?.Invoke();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void AfterCascadeComplete_ReEnablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });
            _matchService.FindAllMatches(_model).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;
            Action spawnCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.SpawnElements(Arg.Any<List<SpawnData>>(), Arg.Do<Action>(cb => spawnCallback = cb));

            _presenter.TrySwap(posA, posB);
            _inputService.ClearReceivedCalls();

            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            spawnCallback?.Invoke();

            _inputService.Received(1).SetInputEnabled(true);
        }

        [Test]
        public void AfterCascadeComplete_IsProcessingFalse()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            SetupSwapWithMatch(posA, posB);

            _fallService.GetEmptyTopPositions(_model).Returns(new List<GridPosition> { new(0, 7) });
            _matchService.FindAllMatches(_model).Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;
            Action spawnCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));
            _view.SpawnElements(Arg.Any<List<SpawnData>>(), Arg.Do<Action>(cb => spawnCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();
            destroyCallback?.Invoke();
            spawnCallback?.Invoke();

            Assert.IsFalse(_presenter.IsProcessing);
        }

        // === Helper ===

        private void SetupSwapWithMatch(GridPosition posA, GridPosition posB)
        {
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, posA).Returns(new List<GridPosition> { posA });
            _matchService.FindMatchesAt(_model, posB).Returns(new List<GridPosition>());
        }
    }
}
