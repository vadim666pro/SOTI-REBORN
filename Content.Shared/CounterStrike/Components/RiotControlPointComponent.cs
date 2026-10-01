namespace Content.Shared.CounterStrike.Components;

[RegisterComponent]
public sealed partial class RiotControlPointComponent : Component
{
    [DataField]
    public float CaptureRadius = 5f;

    public RiotTeam Owner;
}

public enum RiotTeam : byte
{
    None,
    Civilians,
    LawEnforcement,
}