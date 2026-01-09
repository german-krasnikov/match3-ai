// Assets/Tests/EditMode/Features/Board/BoardPresenterTests.cs
using System;
using NUnit.Framework;
using NSubstitute;
using Common;
using Features.Board.Models;
using Features.Board.Presenters;
using Features.Board.Views;

namespace Features.Board.Presenters
{
    [TestFixture]
    public class BoardPresenterTests
    {
        private BoardModel _model;
        private IBoardView _view;
        private BoardPresenter _presenter;

        private const int Width = 8;
        private const int Height = 8;
        private const float CellSize = 1.0f;

        [SetUp]
        public void SetUp()
        {
            _model = new BoardModel(Width, Height);
            _view = Substitute.For<IBoardView>();
            _presenter = new BoardPresenter(_model, _view, CellSize);
        }

        [TearDown]
        public void TearDown()
        {
            _presenter.Dispose();
        }

        [Test]
        public void Constructor_InitializesView()
        {
            _view.Received(1).Initialize(Width, Height, CellSize);
        }

        [Test]
        public void WhenElementAdded_CreatesViewElement()
        {
            var pos = new GridPosition(3, 3);
            var element = new Element(ElementType.Red);

            _model.SetElement(pos, element);

            _view.Received(1).CreateElement(pos, ElementType.Red);
        }

        [Test]
        public void WhenElementRemoved_RemovesViewElement()
        {
            var pos = new GridPosition(2, 2);
            _model.SetElement(pos, new Element(ElementType.Blue));
            _view.ClearReceivedCalls();

            _model.RemoveElement(pos);

            _view.Received(1).RemoveElement(pos);
        }

        [Test]
        public void SyncViewWithModel_ClearsAndRecreatesAllElements()
        {
            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _model.SetElement(new GridPosition(1, 1), new Element(ElementType.Blue));
            _view.ClearReceivedCalls();

            _presenter.SyncViewWithModel();

            _view.Received(1).Clear();
            _view.Received(1).CreateElement(new GridPosition(0, 0), ElementType.Red);
            _view.Received(1).CreateElement(new GridPosition(1, 1), ElementType.Blue);
        }

        [Test]
        public void SyncViewWithModel_SkipsEmptyCells()
        {
            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _view.ClearReceivedCalls();

            _presenter.SyncViewWithModel();

            // Should only call CreateElement once for the one element
            _view.Received(1).CreateElement(Arg.Any<GridPosition>(), Arg.Any<ElementType>());
        }

        [Test]
        public void Dispose_UnsubscribesFromModelEvents()
        {
            _presenter.Dispose();
            _view.ClearReceivedCalls();

            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));

            _view.DidNotReceive().CreateElement(Arg.Any<GridPosition>(), Arg.Any<ElementType>());
        }

        [Test]
        public void Dispose_DoesNotAffectModelRemoveAfterDispose()
        {
            _model.SetElement(new GridPosition(0, 0), new Element(ElementType.Red));
            _presenter.Dispose();
            _view.ClearReceivedCalls();

            _model.RemoveElement(new GridPosition(0, 0));

            _view.DidNotReceive().RemoveElement(Arg.Any<GridPosition>());
        }

        [Test]
        public void MultipleElements_AllCreatedInView()
        {
            var positions = new[]
            {
                new GridPosition(0, 0),
                new GridPosition(1, 0),
                new GridPosition(2, 0)
            };

            foreach (var pos in positions)
            {
                _model.SetElement(pos, new Element(ElementType.Green));
            }

            foreach (var pos in positions)
            {
                _view.Received(1).CreateElement(pos, ElementType.Green);
            }
        }
    }
}
