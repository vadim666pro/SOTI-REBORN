using Content.Server.Audio;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class RandomSoundRuleSystem : GameRuleSystem<RandomSoundRuleComponent>
{
    [Dependency] private readonly ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Started(EntityUid uid, RandomSoundRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        component.NextSoundTime = _random.NextFloat(component.MinInterval, component.MaxInterval);
        component.Elapsed = 0f;
    }

    protected override void ActiveTick(EntityUid uid, RandomSoundRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        component.Elapsed += frameTime;

        if (component.Elapsed < component.NextSoundTime)
            return;

        var sound = new SoundCollectionSpecifier(component.SoundCollection);
        var resolved = _audio.ResolveSound(sound);
        var audioParams = AudioParams.Default.WithVolume(component.Volume);
        _globalSound.PlayAdminGlobal(Filter.Broadcast(), resolved, audioParams, replay: true);

        component.Elapsed = 0f;
        component.NextSoundTime = _random.NextFloat(component.MinInterval, component.MaxInterval);
    }
}
