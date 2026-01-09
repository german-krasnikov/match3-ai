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

            _presenter = new BoardPresenter(
                _model,
                _view,
                CellSize,
                _matchService,
                _inputService);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        // === Constructor Tests ===

        [Test]
        public void Constructor_InitializesView()
        {
            _view.Received(1).Initialize(Width, Height, CellSize);
        }

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

            // Setup match service to return matches (valid swap)
            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition> { posA });

            _presenter.TrySwap(posA, posB);

            // Model should have swapped
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

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition> { posA });

            _presenter.TrySwap(posA, posB);

            _view.Received(1).SwapElements(posA, posB, Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_WithEmptyPosition_DoesNotSwap()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            // posB is empty

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

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition> { posA });

            // Start first swap
            _presenter.TrySwap(posA, posB);

            // Clear received calls
            _view.ClearReceivedCalls();

            // Try second swap while first is in progress
            var posC = new GridPosition(2, 0);
            var posD = new GridPosition(3, 0);
            _model.SetElement(posC, new Element(ElementType.Green));
            _model.SetElement(posD, new Element(ElementType.Yellow));

            _presenter.TrySwap(posC, posD);

            _view.DidNotReceive().SwapElements(posC, posD, Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_SetsIsSwappingTrue()
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
            var elementA = new Element(ElementType.Red);
            var elementB = new Element(ElementType.Blue);
            _model.SetElement(posA, elementA);
            _model.SetElement(posB, elementB);

            // Match service returns NO matches (invalid swap)
            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);

            // After first swap animation completes
            swapCallback?.Invoke();

            // Should call swap again (rollback animation)
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

            // No matches
            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            // Model should be back to original state
            Assert.AreEqual(elementA, _model.GetElement(posA));
            Assert.AreEqual(elementB, _model.GetElement(posB));
        }

        [Test]
        public void TrySwap_MatchFromFirstPosition_IsValid()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            // Only position A creates match
            _matchService.FindMatchesAt(_model, posA)
                .Returns(new List<GridPosition> { posA, new GridPosition(0, 1), new GridPosition(0, 2) });
            _matchService.FindMatchesAt(_model, posB)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            // Should NOT rollback (only one call to SwapElements)
            _view.Received(1).SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_MatchFromSecondPosition_IsValid()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            // Only position B creates match
            _matchService.FindMatchesAt(_model, posA)
                .Returns(new List<GridPosition>());
            _matchService.FindMatchesAt(_model, posB)
                .Returns(new List<GridPosition> { posB, new GridPosition(1, 1), new GridPosition(1, 2) });

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            // Should NOT rollback
            _view.Received(1).SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        // === Input Service Integration ===

        [Test]
        public void WhenSwapRequested_CallsTrySwap()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition> { posA });

            // Simulate input service event
            _inputService.OnSwapRequested += Raise.Event<Action<GridPosition, GridPosition>>(posA, posB);

            _view.Received(1).SwapElements(posA, posB, Arg.Any<Action>());
        }

        [Test]
        public void AfterValidSwap_WithMatches_ReEnablesInputAfterDestroy()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition> { posA });

            Action swapCallback = null;
            Action destroyCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.TrySwap(posA, posB);
            _inputService.ClearReceivedCalls();

            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _inputService.Received(1).SetInputEnabled(true);
        }

        [Test]
        public void AfterRollback_ReEnablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            // No matches
            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
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

            _presenter.TrySwap(posA, posB);
            _inputService.ClearReceivedCalls();

            swapCallback?.Invoke(); // Triggers rollback
            rollbackCallback?.Invoke(); // Completes rollback

            _inputService.Received(1).SetInputEnabled(true);
        }

        // === Dispose Tests ===

        [Test]
        public void Dispose_UnsubscribesFromModelEvents()
        {
            _presenter.Dispose();
            _view.ClearReceivedCalls();

            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));

            _view.DidNotReceive().CreateElement(Arg.Any<GridPosition>(), Arg.Any<ElementType>());
        }

        // === No MatchService Tests ===

        [Test]
        public void TrySwap_WithoutMatchService_AlwaysSucceeds()
        {
            // Create presenter without match service
            _presenter.Dispose();
            _presenter = new BoardPresenter(_model, _view, CellSize, null, _inputService);

            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            // Should only be called once (no rollback)
            _view.Received(1).SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(), Arg.Any<Action>());
        }

        // === Destroy Tests ===

        [Test]
        public void DestroyMatches_CallsViewDestroyElements()
        {
            var matches = new List<GridPosition>
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(2, 0)
            };

            _presenter.DestroyMatches(matches);

            _view.Received(1).DestroyElements(
                Arg.Is<List<GridPosition>>(list => list.Count == 3),
                Arg.Any<Action>());
        }

        [Test]
        public void DestroyMatches_AfterAnimation_RemovesFromModel()
        {
            var pos = new GridPosition(0, 0);
            _model.SetElement(pos, new Element(ElementType.Red));

            Action destroyCallback = null;
            _view.DestroyElements(
                Arg.Any<List<GridPosition>>(),
                Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.DestroyMatches(new List<GridPosition> { pos });
            destroyCallback?.Invoke();

            Assert.IsNull(_model.GetElement(pos));
        }

        [Test]
        public void DestroyMatches_WithEmptyList_CompletesImmediately()
        {
            _presenter.DestroyMatches(new List<GridPosition>());

            _view.DidNotReceive().DestroyElements(
                Arg.Any<List<GridPosition>>(),
                Arg.Any<Action>());
        }

        [Test]
        public void DestroyMatches_FiresOnMatchesDestroyedEvent()
        {
            var pos = new GridPosition(0, 0);
            _model.SetElement(pos, new Element(ElementType.Red));

            bool eventFired = false;
            _presenter.OnMatchesDestroyed += () => eventFired = true;

            Action destroyCallback = null;
            _view.DestroyElements(
                Arg.Any<List<GridPosition>>(),
                Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.DestroyMatches(new List<GridPosition> { pos });
            destroyCallback?.Invoke();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void TrySwap_WhenMatchCreated_CallsDestroyElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, posA)
                .Returns(new List<GridPosition> { posA, new GridPosition(0, 1), new GridPosition(0, 2) });
            _matchService.FindMatchesAt(_model, posB)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            _view.Received(1).DestroyElements(
                Arg.Is<List<GridPosition>>(list => list.Count == 3),
                Arg.Any<Action>());
        }

        [Test]
        public void TrySwap_WhenNoMatch_DoesNotCallDestroyElements()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, Arg.Any<GridPosition>())
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            _view.SwapElements(Arg.Any<GridPosition>(), Arg.Any<GridPosition>(),
                Arg.Do<Action>(cb => swapCallback = cb));

            _presenter.TrySwap(posA, posB);
            swapCallback?.Invoke();

            _view.DidNotReceive().DestroyElements(
                Arg.Any<List<GridPosition>>(),
                Arg.Any<Action>());
        }

        [Test]
        public void AfterDestroyComplete_ReEnablesInput()
        {
            var posA = new GridPosition(0, 0);
            var posB = new GridPosition(1, 0);
            _model.SetElement(posA, new Element(ElementType.Red));
            _model.SetElement(posB, new Element(ElementType.Blue));

            _matchService.FindMatchesAt(_model, posA)
                .Returns(new List<GridPosition> { posA });
            _matchService.FindMatchesAt(_model, posB)
                .Returns(new List<GridPosition>());

            Action swapCallback = null;
            Action destroyCallback = null;

            _view.SwapElements(posA, posB, Arg.Do<Action>(cb => swapCallback = cb));
            _view.DestroyElements(Arg.Any<List<GridPosition>>(), Arg.Do<Action>(cb => destroyCallback = cb));

            _presenter.TrySwap(posA, posB);
            _inputService.ClearReceivedCalls();

            swapCallback?.Invoke();
            destroyCallback?.Invoke();

            _inputService.Received(1).SetInputEnabled(true);
        }
    }
}
