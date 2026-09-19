using Robust.Shared;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;
using Content.Server.Cult.Components;

namespace Content.Server.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooSaveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
    }

    public void SaveTattoo(EntityUid user, SubmitTattooCircuitMessage message)
    {
        if (!EntityManager.HasComponent<CultMemberComponent>(user)) return;

        // Конвертируем NetEntity обратно в EntityUid
        EntityUid targetUid = EntityManager.GetEntity(message.Target);
        if (targetUid == EntityUid.Invalid) return;
        if (targetUid == user) return;

        EntityManager.EnsureComponent<TattooComponent>(targetUid, out var tattooComp);

        var circuit = new TattooCircuit
        {
            Connections = message.Connections,
            DrawingMask = System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message.DrawingData))
        };

        tattooComp.Circuits.Clear();
        tattooComp.Circuits.Add(circuit);
    }
}
