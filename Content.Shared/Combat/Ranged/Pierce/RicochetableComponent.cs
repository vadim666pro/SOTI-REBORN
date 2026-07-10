namespace Content.Shared.Combat.Ranged.Pierce;

[RegisterComponent]
public sealed partial class RicochetableComponent : Component
{
    [DataField("chance")]
    public float Chance = 1f;
}
