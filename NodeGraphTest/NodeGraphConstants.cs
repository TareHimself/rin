namespace NodeGraphTest;

public static class NodeGraphConstants
{
    public static float PinRadius = 6.0f;
    public static float PinPadding = 6;
    public static float PinDiameter => PinRadius * 2f;
    public static float PinDiameterWithPadding => PinDiameter + (PinPadding * 2f);
}