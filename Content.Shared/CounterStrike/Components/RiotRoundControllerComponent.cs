namespace Content.Shared.CounterStrike.Components;

[RegisterComponent]
public sealed partial class RiotRoundControllerComponent : Component
{
    [DataField]
    public float RoundDuration = 300f;

    [DataField]
    public float SwatArrivalDelay = 180f;

    public float TimeRemaining;
    public float TimeElapsed;
    public float CaptureCheckTimer;
    public bool SwatSpawned;
    public bool RoundEnded;
    public bool MissingPointWarned;
}