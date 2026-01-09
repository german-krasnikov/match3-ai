// Assets/Tests/EditMode/Features/Board/SpawnServiceTests.cs
using System.Collections.Generic;
using NUnit.Framework;
using Common;
using Features.Board.Services;

namespace Features.Board.Services
{
    [TestFixture]
    public class SpawnServiceTests
    {
        private SpawnService _service;
        private const int ElementTypeCount = 5;
        private const int TestSeed = 42;

        [SetUp]
        public void SetUp()
        {
            _service = new SpawnService(ElementTypeCount, TestSeed);
        }

        [Test]
        public void GetRandomElement_ReturnsValidType()
        {
            var result = _service.GetRandomElement();

            Assert.AreNotEqual(ElementType.None, result);
            Assert.IsTrue((int)result >= 1 && (int)result <= ElementTypeCount);
        }

        [Test]
        public void GetRandomElement_ReturnsVariousTypes()
        {
            var types = new HashSet<ElementType>();
            var service = new SpawnService(ElementTypeCount);

            for (int i = 0; i < 100; i++)
            {
                types.Add(service.GetRandomElement());
            }

            Assert.Greater(types.Count, 1, "Should return various element types");
        }

        [Test]
        public void GetRandomElementExcluding_ExcludesSpecifiedTypes()
        {
            var excluded = new[] { ElementType.Red, ElementType.Blue };

            for (int i = 0; i < 50; i++)
            {
                var result = _service.GetRandomElementExcluding(excluded);

                Assert.AreNotEqual(ElementType.Red, result);
                Assert.AreNotEqual(ElementType.Blue, result);
                Assert.AreNotEqual(ElementType.None, result);
            }
        }

        [Test]
        public void GetRandomElementExcluding_WhenAllExcluded_ReturnsNone()
        {
            var allTypes = new[]
            {
                ElementType.Red,
                ElementType.Blue,
                ElementType.Green,
                ElementType.Yellow,
                ElementType.Purple
            };

            var result = _service.GetRandomElementExcluding(allTypes);

            Assert.AreEqual(ElementType.None, result);
        }

        [Test]
        public void GetRandomElementExcluding_WhenEmptyExcluded_ReturnsValidType()
        {
            var result = _service.GetRandomElementExcluding(new ElementType[0]);

            Assert.AreNotEqual(ElementType.None, result);
        }

        [Test]
        public void Constructor_WithSeed_ProducesDeterministicResults()
        {
            var service1 = new SpawnService(ElementTypeCount, 123);
            var service2 = new SpawnService(ElementTypeCount, 123);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(service1.GetRandomElement(), service2.GetRandomElement());
            }
        }

        [Test]
        public void GetRandomElement_NeverReturnsNone()
        {
            var service = new SpawnService(ElementTypeCount);

            for (int i = 0; i < 100; i++)
            {
                var result = service.GetRandomElement();
                Assert.AreNotEqual(ElementType.None, result);
            }
        }
    }
}
