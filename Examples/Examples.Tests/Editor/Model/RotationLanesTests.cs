using System.Numerics;
using Examples.Editor.Model;

namespace Examples.Tests.Editor.Model;

public class RotationLanesTests
{
    [Test]
    public void SettingOneAngleKeepsTheOtherAnglesAsTheyWere()
    {
        var stored = Quaternion.Identity;
        var lanes = new RotationLanes(() => stored, value => stored = value);

        lanes.Set(0, 90f);
        lanes.Set(1, 45f);

        Assert.That(lanes.Get(0), Is.EqualTo(90f));
        Assert.That(lanes.Get(1), Is.EqualTo(45f));
        Assert.That(Quaternion.Dot(stored, FieldLanes.FromEuler(new Vector3(90f, 45f, 0f))), Is.EqualTo(1f).Within(1e-5f));
    }

    [Test]
    public void ARotationChangedElsewhereIsReadAgain()
    {
        var stored = Quaternion.Identity;
        var lanes = new RotationLanes(() => stored, value => stored = value);
        lanes.Set(2, 10f);

        stored = FieldLanes.FromEuler(new Vector3(0f, 30f, 0f));

        Assert.That(lanes.Get(1), Is.EqualTo(30f).Within(1e-3f));
        Assert.That(lanes.Get(2), Is.EqualTo(0f).Within(1e-3f));
    }
}
