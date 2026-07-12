using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vehicle.Components;

/// <summary>
/// Основной компонент транспорта.
/// Управление: W/S — линейное ускорение, A/D — поворот.
/// Физика основана на инерции и плавном Lerp.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EntitySystems.VehicleSystem))]
public sealed partial class VehicleComponent : Component
{
    // ── Состояние двигателя ───────────────────────────────────────────────

    /// <summary>Двигатель запущен (водитель пристёгнут).</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool EngineRunning;

    // ── Параметры движения (настраиваются в YAML) ─────────────────────────

    /// <summary>Максимальная скорость (в тайлах/сек).</summary>
    [DataField]
    public float MaxSpeed = 16f;

    /// <summary>
    /// Скорость разгона (тайлов/сек²). Используется в MathHelper.MoveTowards
    /// для линейного плавного набора скорости.
    /// </summary>
    [DataField]
    public float Acceleration = 6f;

    /// <summary>
    /// Множитель замедления (трение) при отпускании клавиш.
    /// Формула: velocity *= (1 - Friction * frameTime).
    /// Большее значение = быстрее тормозит.
    /// </summary>
    [DataField]
    public float Friction = 1.5f;

    /// <summary>
    /// Скорость замедления при накате (когда клавиши отпущены, без ручника).
    /// Должна быть меньше BrakeDeceleration для длинного наката.
    /// </summary>
    [DataField]
    public float Deceleration = 3f;

    /// <summary>
    /// Скорость торможения при зажатом ручнике (высокое значение = быстрая остановка).
    /// </summary>
    [DataField]
    public float BrakeDeceleration = 18f;

    /// <summary>
    /// Минимальная скорость. Ниже этого значения транспорт считается
    /// остановившимся и скорость обнуляется.
    /// </summary>
    [DataField]
    public float StopThreshold = 0.05f;

    /// <summary>
    /// Максимальная угловая скорость поворота (радианы/сек).
    /// Угол изменяется плавно через ShortestAngleDistance + Clamp.
    /// </summary>
    [DataField]
    public float MaxTurnSpeed = 4.5f;

    /// <summary>
    /// Коэффициент сцепления при дрифте (0 = полный занос, 1 = нет дрифта).
    /// Боковая скорость плавно гасится через Lerp(current, 0, DriftGrip * frameTime).
    /// Чем меньше значение — тем сильнее занос.
    /// </summary>
    [DataField]
    public float DriftGrip = 0.7f;

    /// <summary>
    /// Коэффициент сцепления во время ручника (значительно ниже DriftGrip).
    /// Обеспечивает сильный занос при торможении ручником.
    /// </summary>
    [DataField]
    public float DriftGripBrake = 0.15f;

    /// <summary>Скорость заднего хода (в тайлах/сек).</summary>
    [DataField]
    public float ReverseSpeed = 8f;

    /// <summary>Множитель ускорения при заднем ходе.</summary>
    [DataField]
    public float ReverseAcceleration = 3f;

    // ── Runtime-состояние ─────────────────────────────────────────────────

    /// <summary>Текущая рассчитанная скорость (обновляется каждый тик).</summary>
    public float CurrentSpeed;

    /// <summary>Текущая угловая скорость поворота (накапливается плавно).</summary>
    public float CurrentAngularVelocity;

    /// <summary>Ручник активен (флаг состояния).</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool HandbrakeActive;

    /// <summary>ID прототипа экшена ручника для хотбара.</summary>
    [DataField]
    public EntProtoId HandbrakeAction = "ActionToggleVehicleHandbrake";

    /// <summary>EntityUID экшена ручника (создаётся при посадке водителя).</summary>
    [DataField]
    public EntityUid? HandbrakeActionEntity;

    // ── Задержка входа ─────────────────────────────────────────────────────

    /// <summary>Задержка (сек) перед входом в транспорт (DoAfter progress bar).</summary>
    [DataField]
    public float EnterDelay = 3.0f;

    // ── Пассажирские места ─────────────────────────────────────────────────

    [DataField]
    public int PassengerSeatCount = 3;

    [DataField]
    public List<Vector2> PassengerOffsets = new()
    {
        new(-0.3f, -0.6f),
        new(0.3f, -0.6f),
        new(0.0f, -0.6f),
    };

    public List<EntityUid> PassengerSeatEntities = new();

    // ── Звуки ─────────────────────────────────────────────────────────────

    [DataField] public SoundSpecifier? StartupSound;
    [DataField] public SoundSpecifier? ShutoffSound;
    [DataField] public SoundSpecifier? IdleSound;
    [DataField] public SoundSpecifier? GazSound;

    /// <summary>Текущее состояние звука двигателя.</summary>
    public VehicleSoundState CurrentSoundState = VehicleSoundState.None;

    /// <summary>EntityUID текущего зацикленного звука.</summary>
    public EntityUid? LoopSoundEntity;
}

/// <summary>
/// Компонент ручного тормоза.
/// Активируется по удержанию пробела.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EntitySystems.VehicleSystem))]
public sealed partial class HandbrakeComponent : Component
{
    /// <summary>Ручник включён.</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Engaged;

    [DataField] public SoundSpecifier? EngageSound;
    [DataField] public SoundSpecifier? ReleaseSound;
}

/// <summary>
/// Компонент фар — переключаемый свет через Action System (хотбар).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(EntitySystems.VehicleSystem))]
public sealed partial class VehicleHeadlightsComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleVehicleHeadlights";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    [DataField] public Color LightColor = new(1f, 1f, 0.9f);
    [DataField] public float LightRadius = 8f;
    [DataField] public float LightEnergy = 1.0f;
    [DataField] public float LightSoftness = 1f;
    [DataField] public SoundSpecifier? ToggleSound;
}

public enum VehicleSoundState : byte
{
    None = 0,
    Idle,
    Gaz,
}
