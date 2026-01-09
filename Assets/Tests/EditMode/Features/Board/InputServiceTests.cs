// Assets/Tests/EditMode/Features/Board/InputServiceTests.cs
using NUnit.Framework;
using Common;
using Features.Board.Services;

namespace Features.Board.Services
{
    [TestFixture]
    public class InputServiceTests
    {
        private InputService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new InputService();
        }

        [Test]
        public void AreAdjacent_HorizontalNeighbors_ReturnsTrue()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(3, 3);
            Assert.IsTrue(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void AreAdjacent_VerticalNeighbors_ReturnsTrue()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(2, 4);
            Assert.IsTrue(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void AreAdjacent_DiagonalNeighbors_ReturnsFalse()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(3, 4);
            Assert.IsFalse(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void AreAdjacent_SamePosition_ReturnsFalse()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(2, 3);
            Assert.IsFalse(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void AreAdjacent_TwoApart_ReturnsFalse()
        {
            var a = new GridPosition(2, 3);
            var b = new GridPosition(4, 3);
            Assert.IsFalse(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void AreAdjacent_LeftNeighbor_ReturnsTrue()
        {
            var a = new GridPosition(3, 3);
            var b = new GridPosition(2, 3);
            Assert.IsTrue(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void AreAdjacent_BottomNeighbor_ReturnsTrue()
        {
            var a = new GridPosition(3, 3);
            var b = new GridPosition(3, 2);
            Assert.IsTrue(InputService.AreAdjacent(a, b));
        }

        [Test]
        public void RequestSwap_AdjacentPositions_FiresOnSwapRequested()
        {
            GridPosition receivedFrom = GridPosition.Invalid;
            GridPosition receivedTo = GridPosition.Invalid;
            _service.OnSwapRequested += (from, to) =>
            {
                receivedFrom = from;
                receivedTo = to;
            };

            var from = new GridPosition(2, 3);
            var to = new GridPosition(3, 3);

            _service.RequestSwap(from, to);

            Assert.AreEqual(from, receivedFrom);
            Assert.AreEqual(to, receivedTo);
        }

        [Test]
        public void RequestSwap_DiagonalPositions_DoesNotFireEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            _service.RequestSwap(new GridPosition(2, 3), new GridPosition(3, 4));

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestSwap_SamePosition_DoesNotFireEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            var pos = new GridPosition(2, 3);
            _service.RequestSwap(pos, pos);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestSwap_InvalidFromPosition_DoesNotFireEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            _service.RequestSwap(GridPosition.Invalid, new GridPosition(3, 3));

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestSwap_InvalidToPosition_DoesNotFireEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            _service.RequestSwap(new GridPosition(2, 3), GridPosition.Invalid);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestSwap_TwoApartPositions_DoesNotFireEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            _service.RequestSwap(new GridPosition(2, 3), new GridPosition(4, 3));

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestSwap_WhenInputDisabled_DoesNotFireEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            _service.SetInputEnabled(false);
            _service.RequestSwap(new GridPosition(2, 3), new GridPosition(3, 3));

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestSwap_WhenInputReEnabled_FiresEvent()
        {
            bool eventFired = false;
            _service.OnSwapRequested += (from, to) => eventFired = true;

            _service.SetInputEnabled(false);
            _service.SetInputEnabled(true);
            _service.RequestSwap(new GridPosition(2, 3), new GridPosition(3, 3));

            Assert.IsTrue(eventFired);
        }

        [TestCase(0, 0, 1, 0, true)]
        [TestCase(0, 0, 0, 1, true)]
        [TestCase(7, 7, 6, 7, true)]
        [TestCase(7, 7, 7, 6, true)]
        [TestCase(0, 0, 1, 1, false)]
        [TestCase(3, 3, 5, 3, false)]
        [TestCase(3, 3, 3, 5, false)]
        public void AreAdjacent_VariousPositions_ReturnsExpected(
            int ax, int ay, int bx, int by, bool expected)
        {
            var a = new GridPosition(ax, ay);
            var b = new GridPosition(bx, by);
            Assert.AreEqual(expected, InputService.AreAdjacent(a, b));
        }
    }
}
