using LetterHunter.Environment;
using NUnit.Framework;
using UnityEngine;

public sealed class ParallaxLayer2DTests
{
    [Test]
    public void CameraMotionAppliesIndependentFactorsAndPreservesDepth()
    {
        var result = ParallaxLayer2D.CalculatePosition(new Vector3(2, 3, 8),
            new Vector3(10, 5, -10), new Vector3(30, 15, -8), new Vector2(.25f, .8f), 0);
        Assert.AreEqual(new Vector3(7, 11, 8), result);
    }

    [TestCase(155f, 160f)]
    [TestCase(-155f, -160f)]
    public void RepeatedSegmentWrapsInBothDirections(float cameraX, float expectedX)
    {
        Assert.AreEqual(expectedX, ParallaxLayer2D.CalculatePosition(Vector3.zero,
            Vector3.zero, new Vector3(cameraX, 0, 0), Vector2.zero, 80).x);
    }

    [Test]
    public void ReturningCameraRestoresAuthoredPositionWithoutAccumulatedDrift()
    {
        var origin = new Vector3(3, 4, 5);
        ParallaxLayer2D.CalculatePosition(origin, Vector3.zero, new Vector3(900, 70, 0),
            new Vector2(.7f, 1), 80);
        Assert.AreEqual(origin, ParallaxLayer2D.CalculatePosition(origin, Vector3.zero,
            Vector3.zero, new Vector2(.7f, 1), 80));
    }
}
