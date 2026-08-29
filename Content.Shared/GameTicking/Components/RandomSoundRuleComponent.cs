using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.GameTicking.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RandomSoundRuleComponent : Component
{
    [DataField("soundCollection", required: true)]
    public string SoundCollection = default!;

    [DataField("minInterval")]
    public float MinInterval = 5f;

    [DataField("maxInterval")]
    public float MaxInterval = 25f;

    [DataField("volume")]
    public float Volume = -8f;

    [ViewVariables]
    public float Elapsed;

    [ViewVariables]
    public float NextSoundTime;
}
