namespace Rin.Engine;

public interface IBinarySerializable
{
    public void BinarySerialize(Stream output);
    public void BinaryDeserialize(Stream input);
}