using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Cult;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class TatooPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField]
    public string Name { get; } = default!;

    [DataField]
    public int Tier;

    // [DataField]
    // public Dictionary<string, BasePlagueEffect> Effects = new();
}
