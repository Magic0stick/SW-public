using Robust.Shared;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

/// <summary>
/// Маркер для предметов, которыми можно наносить татуировки.
/// </summary>
[RegisterComponent]
[NetSerializable, Serializable]
public sealed partial class TattooToolComponent : Component
{
    // Можно добавить параметры, например, качество инструмента или тип используемой крови
}
