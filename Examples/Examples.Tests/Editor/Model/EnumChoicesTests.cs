using Examples.Editor.Model;
using Rin.World;

namespace Examples.Tests.Editor.Model;

public class EnumChoicesTests
{
    private enum Small : byte
    {
        First = 1,
        Second = 7,
        Last = 200
    }

    private enum Signed : short
    {
        Below = -1,
        Zero = 0
    }

    [Test]
    public void ValuesListsEveryNamedValueInAscendingOrder()
    {
        var choices = new EnumChoices(FieldType.Of<Small>(FieldKind.Enum));

        Assert.That(choices.Values, Is.EqualTo(new ulong[] { 1, 7, 200 }));
    }

    [Test]
    public void AValueIsReadAndWrittenAsTheFieldsOwnBytes()
    {
        var choices = new EnumChoices(FieldType.Of<Signed>(FieldKind.Enum));
        var bytes = new byte[] { 0, 0, 0xAB };

        choices.Write(bytes, choices.Values.Single(value => choices.NameOf(value) == "Below"));

        Assert.That(BitConverter.ToInt16(bytes), Is.EqualTo((short)Signed.Below));
        Assert.That(choices.NameOf(choices.Read(bytes)), Is.EqualTo("Below"));
        Assert.That(bytes[2], Is.EqualTo(0xAB));
    }

    [Test]
    public void AnUnnamedValueShowsAsItsNumber()
    {
        var choices = new EnumChoices(FieldType.Of<Small>(FieldKind.Enum));

        Assert.That(choices.NameOf(3), Is.EqualTo("3"));
    }
}
