using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

// [RegisterComponent] обязателен, чтобы Robust зарегистрировал его в ECS
[NetSerializable, Serializable]
public sealed partial class TattooComponent : Component
{
    // Список закрашенных пикселей 32x32 на теле этого существа
    [DataField("tattooPixels")]
    public List<Vector2i> TattooPixels = new();
}
