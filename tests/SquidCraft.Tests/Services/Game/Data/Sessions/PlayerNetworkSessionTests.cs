using System.Collections.Generic;
using System.Numerics;
using SquidCraft.Services.Game.Data.Sessions;

namespace SquidCraft.Tests.Services.Game.Data.Sessions;

[TestFixture]
public class PlayerNetworkSessionTests
{
    [Test]
    public void Position_SetNewValue_RaisesPositionChangedEvent()
    {
        var session = new PlayerNetworkSession();
        var receivedPositions = new List<Vector3>();
        session.OnPositionChanged += receivedPositions.Add;

        var expected = new Vector3(1f, 2f, 3f);

        session.Position = expected;

        Assert.That(receivedPositions, Has.Count.EqualTo(1));
        Assert.That(receivedPositions[0], Is.EqualTo(expected));
    }

    [Test]
    public void Position_SetSameValue_DoesNotRaiseEvent()
    {
        var session = new PlayerNetworkSession();
        var receivedPositions = new List<Vector3>();
        session.OnPositionChanged += receivedPositions.Add;

        var original = new Vector3(4f, 5f, 6f);

        session.Position = original;
        session.Position = original;

        Assert.That(receivedPositions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Facing_SetNonUnitVector_NormalizesAndRaisesEvent()
    {
        var session = new PlayerNetworkSession();
        Vector3? raisedFacing = null;
        session.OnFacingChanged += facing => raisedFacing = facing;

        session.Facing = new Vector3(0f, 0f, 5f);

        var expected = new Vector3(0f, 0f, 1f);

        Assert.That(raisedFacing, Is.EqualTo(expected));
        Assert.That(session.Facing, Is.EqualTo(expected));
    }

    [Test]
    public void Facing_SetVectorWithSameDirection_DoesNotRaiseAdditionalEvent()
    {
        var session = new PlayerNetworkSession();
        var raisedCount = 0;
        session.OnFacingChanged += _ => raisedCount++;

        session.Facing = new Vector3(1f, 0f, 0f);
        session.Facing = new Vector3(2f, 0f, 0f);

        Assert.That(raisedCount, Is.EqualTo(1));
    }
}
