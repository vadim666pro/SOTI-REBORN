using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Soti.Audio;

// 1. РЕГИСТРИРУЕМ КОМПОНЕНТ ДЛЯ YAML
[RegisterComponent]
public sealed partial class SoundOnActionComponent : Component
{
    [DataField("sound", required: true)]
    public SoundSpecifier Sound = default!;
}

// 2. ПИШЕМ СИСТЕМУ ОБРАБОТКИ
public sealed class SoundOnActionSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Теперь каждый ивент вызывает свой собственный правильный метод
        SubscribeLocalEvent<SoundOnActionComponent, InstantActionEvent>(OnInstantAction);
        SubscribeLocalEvent<SoundOnActionComponent, WorldTargetActionEvent>(OnWorldTargetAction);
        SubscribeLocalEvent<SoundOnActionComponent, EntityTargetActionEvent>(OnEntityTargetAction);
    }

    private void OnInstantAction(EntityUid uid, SoundOnActionComponent component, InstantActionEvent args)
    {
        // Не проверяем args.Handled, чтобы звук играл в любом случае!
        PlayActionSound(component, args.Performer);
    }

    private void OnWorldTargetAction(EntityUid uid, SoundOnActionComponent component, WorldTargetActionEvent args)
    {
        PlayActionSound(component, args.Performer);
    }

    private void OnEntityTargetAction(EntityUid uid, SoundOnActionComponent component, EntityTargetActionEvent args)
    {
        PlayActionSound(component, args.Performer);
    }

    // Общая функция для воспроизведения звука
    private void PlayActionSound(SoundOnActionComponent component, EntityUid performer)
    {
        _audio.PlayPvs(component.Sound, performer);
    }
}

