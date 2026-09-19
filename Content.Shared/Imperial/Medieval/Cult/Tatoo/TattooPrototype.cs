using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype("tattoo")]
public sealed partial class TattooPrototype : IPrototype
{
    /// <inheritdoc/>

    [IdDataField] public string ID { get; } = default!;

    [DataField("name")] public LocId Name;
    [DataField("description")] public LocId Description;

    [DataField("basePower")] public float BasePower = 10f;

    [DataField("tierMultipliers")]
    public Dictionary<int, float> TierMultipliers = new()
    {
        { 1, 1.0f }, { 2, 1.2f }, { 3, 1.5f },
        { 4, 2.0f }, { 5, 2.5f }, { 6, 3.0f }
    };

    [DataField("allowedSlots")]
    public List<TattooSlot>? AllowedSlots;

    // Визуал тату для каждого слота (RSI path + state)
    // Ключ = слот, значение = спрайт
    [DataField("slotVisuals")]
    public Dictionary<TattooSlot, TattooVisual> SlotVisuals = new();

    // Визуал анимации рисования (один для всех слотов, или можно тоже по слотам)
    [DataField("drawEffect")]
    public string? DrawEffectPrototype; // ID прототипа эффекта рисования



}
[DataDefinition]
public sealed partial class TattooVisual
{
    // Путь к RSI файлу (например, "/Textures/Imperial/Cult/Tattoos/torso.rsi")
    [DataField("rsi")] public string RsiPath = default!;

    // State внутри RSI (например, "ward_tier3")
    [DataField("state")] public string RsiState = default!;
}

public sealed class Lol
{
    public Lol()
    {
    }
}
