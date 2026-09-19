using Robust.Shared;
using Robust.Shared.Network;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;
using Content.Server.Cult.Components;
using Robust.Shared.Player;

namespace Content.Server.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooInteractionSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actors = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void TryStartTattooing(EntityUid user, EntityUid target, EntityUid tool)
    {
        if (!EntityManager.HasComponent<CultMemberComponent>(user))
        {
            return;
        }

        if (!EntityManager.HasComponent<TattooToolComponent>(tool))
        {
            return;
        }

        if (user == target) return;

        if (_actors.TryGetSession(user, out var session) && session is not null)
        {
            var nettarget = EntityManager.GetNetEntity(target);
            var message = new OpenTattooWindowMessage { Target = nettarget };
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);

        }
    }
}
