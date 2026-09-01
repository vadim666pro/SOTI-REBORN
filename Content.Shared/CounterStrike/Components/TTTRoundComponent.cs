using Robust.Shared.GameStates;
using Content.Shared.GameTicking.Components;

namespace Content.Shared.CounterStrike.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class TTTRuleComponent : Component
{
    public const float FreePhaseDuration = 150f; // 2:30
    public const int PoliceCount = 4;
    public const float PolicePhaseDuration = 90f;

    public TTTPhase Phase = TTTPhase.FreePhase;
    public float Timer = FreePhaseDuration;
    public bool PoliceSpawned;

    public float PolicePhaseTimer = 90f;
    public bool PolicePhaseActive;
}

public enum TTTPhase : byte
{
    FreePhase,
    PoliceSpawn,
    PolicePhase
}
