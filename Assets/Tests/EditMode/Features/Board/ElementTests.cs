// Assets/Tests/EditMode/Features/Board/ElementTests.cs
using NUnit.Framework;
using Common;
using Features.Board.Models;

namespace Features.Board.Models
{
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
        public void CanMatch_WhenNotNone_ReturnsTrue()
        {
            var element = new Element(ElementType.Blue);

            Assert.IsTrue(element.CanMatch);
        }

        [Test]
        public void CanMatch_WhenNone_ReturnsFalse()
        {
            var element = new Element(ElementType.None);

            Assert.IsFalse(element.CanMatch);
        }

        [Test]
        public void Matches_SameType_ReturnsTrue()
        {
            var element1 = new Element(ElementType.Green);
            var element2 = new Element(ElementType.Green);

            Assert.IsTrue(element1.Matches(element2));
        }

        [Test]
        public void Matches_DifferentType_ReturnsFalse()
        {
            var element1 = new Element(ElementType.Red);
            var element2 = new Element(ElementType.Blue);

            Assert.IsFalse(element1.Matches(element2));
        }

        [Test]
        public void Matches_WithNull_ReturnsFalse()
        {
            var element = new Element(ElementType.Yellow);

            Assert.IsFalse(element.Matches(null));
        }

        [Test]
        public void Matches_BothNone_ReturnsFalse()
        {
            var element1 = new Element(ElementType.None);
            var element2 = new Element(ElementType.None);

            Assert.IsFalse(element1.Matches(element2));
        }
    }
}
