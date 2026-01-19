using Content.Shared.Imperial.Medieval.Cult;

namespace Content.Server.Imperial.Medieval.Cult.Bloodspells.medium;

/// <summary>
/// This handles...
/// </summary>
public sealed class BloodShieldSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BloodShieldComponent, ShieldAddAction>(AddShield);
    }

    private void AddShield(EntityUid uid, BloodShieldComponent component, ShieldAddAction action)
    {

    }
}
