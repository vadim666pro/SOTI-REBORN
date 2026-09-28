using Content.Shared.Damage;

namespace Content.Server.CounterStrike.Trains;

[RegisterComponent]
public sealed partial class CsTrainKillZoneComponent : Component
{
    [DataField]
    public float Speed = 4f;

    [DataField]
    public DamageSpecifier Damage = new();

    public HashSet<EntityUid> HitEntities = new();
}