using NUnit.Framework;
using UnityEngine;

public class SampleTests
{
    [Test]
    public void SimplePassingTest()
    {
        Assert.AreEqual(4, 2 + 2);
    }

    [Test]
    public void VectorAdditionTest()
    {
        var a = new Vector3(1, 2, 3);
        var b = new Vector3(4, 5, 6);
        var result = a + b;
        
        Assert.AreEqual(new Vector3(5, 7, 9), result);
    }

    [Test]
    public void GameObjectCreationTest()
    {
        var go = new GameObject("TestObject");
        
        Assert.IsNotNull(go);
        Assert.AreEqual("TestObject", go.name);
        
        Object.DestroyImmediate(go);
    }
}
