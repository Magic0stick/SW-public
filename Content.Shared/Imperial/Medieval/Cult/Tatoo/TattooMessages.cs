using Robust.Shared.Serialization;
using System.Collections.Generic;
using System.Numerics;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

/// <summary>
/// Сообщение для открытия окна рисования татуировки на клиенте.
/// </summary>
[NetSerializable, Serializable]
public sealed class OpenTattooWindowMessage : EntityEventArgs
{
    public NetEntity Target;
}

/// <summary>
/// Сообщение для отправки завершенной схемы татуировки на сервер.
/// А вы знали что магия на самом деле просто схемотехника?
/// </summary>
[Serializable, NetSerializable]
public sealed class SubmitTattooCircuitMessage : EntityEventArgs
{
    public NetEntity Target { get; set; }

    // Передаем сетку закрашенных точек игрока
    public List<Vector2i> Pixels { get; set; } = new();
}
