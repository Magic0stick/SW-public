using Robust.Shared;
using Robust.Shared.Network;
using Content.Shared.Imperial.Medieval.Cult.Tatoo;
using Content.Shared.Cult.Components;
using Content.Shared.Interaction;
using Robust.Shared.Player;
using Robust.Shared;

namespace Content.Shared.Imperial.Medieval.Cult.Tatoo;

public sealed class TattooInteractionSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actors = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TattooToolComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid tool, TattooToolComponent toolComp, AfterInteractEvent args)
    {
        Log.Info($"[TattooDebug] AfterInteract detected. Tool: {tool}, Target: {args.Target}");
        if (args.Handled || args.Target is null || !args.CanReach)
        {
            Log.Info($"[TattooDebug] Interaction rejected. Handled: {args.Handled}, TargetNull: {args.Target == null}, CanReach: {args.CanReach}");
            return;
        }

        TryStartTattooing(args.User, args.Target.Value, tool);
        args.Handled = true;
    }

    public void TryStartTattooing(EntityUid user, EntityUid target, EntityUid tool)
    {
        Log.Info($"[TattooDebug] TryStartTattooing. User: {user}, Target: {target}, Tool: {tool}");

        if (!EntityManager.HasComponent<CultMemberComponent>(user))
        {
            Log.Info($"[TattooDebug] User {user} is NOT a cult member.");
            return;
        }

        if (user == target) return;

        var session = _actors.TryGetSession(user, out var s) ? s : null;
        if (session != null)
        {
            var nettarget = EntityManager.GetNetEntity(target);
            var message = new OpenTattooWindowMessage { Target = nettarget };
            EntityManager.EntityNetManager?.SendSystemNetworkMessage(message, session.Channel);
            Log.Info($"[TattooDebug] OpenTattooWindowMessage sent to session {session.Channel}");
        }
        else
        {
            Log.Info($"[TattooDebug] Could not find session for user {user}");
        }
    }
}
