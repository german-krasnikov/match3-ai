using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayModeTests
{
    [UnityTest]
    public IEnumerator GameObjectMovesOverTime()
    {
        var go = new GameObject("MovingObject");
        var startPos = go.transform.position;
        
        go.transform.position += Vector3.right;
        yield return null;
        
        Assert.AreNotEqual(startPos, go.transform.position);
        
        Object.Destroy(go);
    }

    [UnityTest]
    public IEnumerator WaitForSecondsWorks()
    {
        float startTime = Time.time;
        
        yield return new WaitForSeconds(0.1f);
        
        Assert.Greater(Time.time, startTime);
    }
}
