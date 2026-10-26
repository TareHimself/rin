namespace Rin.Core.Tests;

public class NativeExceptionTests
{
    private enum FakeResult
    {
        Ok = 0,
        DeviceLost = 3
    }

    [Test]
    public void Message_NamesTheFunctionLibraryAndCode()
    {
        var exception = new NativeException("Rin.Fake.Native", "fakeCreate", FakeResult.DeviceLost);

        Assert.Multiple(() =>
        {
            Assert.That(exception.Message, Is.EqualTo("fakeCreate in Rin.Fake.Native failed with DeviceLost"));
            Assert.That(exception.Library, Is.EqualTo("Rin.Fake.Native"));
            Assert.That(exception.Function, Is.EqualTo("fakeCreate"));
            Assert.That(exception.Code, Is.EqualTo(FakeResult.DeviceLost));
        });
    }
}
