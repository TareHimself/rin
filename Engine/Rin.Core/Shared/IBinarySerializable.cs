namespace Rin.Core.Shared;

public interface IBinarySerializable
{
    public void BinarySerialize(Stream output);
    public void BinaryDeserialize(Stream input);
}