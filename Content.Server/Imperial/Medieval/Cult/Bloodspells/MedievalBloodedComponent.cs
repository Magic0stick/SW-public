using Robust.Shared.Serialization;

namespace Content.Server.Imperial.Medieval.Cult.Bloodspells;

[RegisterComponent]
[Serializable]
public sealed partial class MedievalBloodedComponent : Component
{
    [DataField("blood"), ViewVariables(VVAccess.ReadWrite)]
    public int Blood;
}
