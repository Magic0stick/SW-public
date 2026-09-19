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
/// </summary>
[NetSerializable, Serializable]
public sealed class SubmitTattooCircuitMessage : EntityEventArgs
{
    public NetEntity Target;
    public List<TattooConnection> Connections = new();
    public List<Vector2> DrawingData = new();
}
