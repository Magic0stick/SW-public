using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

[NetSerializable, Serializable]
public enum TattooSlot
{
    Head,          // Голова (1)
    TorsoUpper,    // Торс верх (1)
    TorsoLower,    // Торс низ (1)
    ArmLeft,       // Левая рука (1)
    ArmRight,      // Правая рука (1)
    LegLeft,       // Левая нога (1)
    LegRight       // Правая нога (1)
}

[NetSerializable, Serializable]
public struct TattooConnection
{
    public TattooSlot From;
    public TattooSlot To;

    public TattooConnection(TattooSlot from, TattooSlot to)
    {
        From = from;
        To = to;
    }
}
