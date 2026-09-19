using System.Collections.Generic;
using System.Numerics;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

/// <summary>
/// Данные о кровавой схеме татуировки.
/// Вместо простых пикселей мы храним граф связей между точками тела.
/// </summary>
public sealed partial class TattooCircuit
{
    // Список активных соединений между слотами.
    public List<TattooConnection> Connections = new();

    // Дополнительные "логические" узлы или модификаторы.
    public List<TattooNode> InternalNodes = new();

    // Хранение самого рисунка для визуализации (попиксельная маска)
    public byte[] DrawingMask = System.Array.Empty<byte>();
}

public sealed partial class TattooNode
{
    public Vector2 Position;
    public string NodeType = string.Empty; // Тип логического элемента (например, "Amplifier", "Inverter")
}
