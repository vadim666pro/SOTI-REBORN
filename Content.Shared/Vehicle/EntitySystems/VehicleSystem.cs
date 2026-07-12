using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Toggleable;
using Content.Shared.Vehicle.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vehicle.EntitySystems;

/// <summary>
/// Система управления транспортом.
/// Физика: плавный разгон/торможение через Lerp + инерция + дрифт.
/// Управление: W/S — линейное, A/D — угловое, Пробел — ручник, Action — фары.
/// </summary>
public abstract class VehicleSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    private EntityQuery<InputMoverComponent> _moverQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _moverQuery = GetEntityQuery<InputMoverComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        // ── Подписки на события ─────────────────────────────────────────────
        SubscribeLocalEvent<VehicleComponent, ComponentStartup>(OnVehicleStartup);
        SubscribeLocalEvent<VehicleComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<VehicleComponent, VehicleEnterDoAfterEvent>(OnEnterDoAfter);
        SubscribeLocalEvent<VehicleComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<VehicleComponent, VehicleDragDoAfterEvent>(OnDragDoAfter);
        SubscribeLocalEvent<VehicleSeatComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<VehicleSeatComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<VehicleHeadlightsComponent, ToggleActionEvent>(OnHeadlightsToggleAction);
        SubscribeLocalEvent<VehicleComponent, ToggleActionEvent>(OnHandbrakeToggleAction);

        // ── Привязка клавиш ────────────────────────────────────────────────
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.VehicleHandbrake, new VehicleInputCmdHandler(this, VehicleInputCmdType.Handbrake))
            .Register<VehicleSystem>();
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ОБРАБОТКА ВВОДА
    // ══════════════════════════════════════════════════════════════════════

    public void HandleVehicleInput(ICommonSession? session, VehicleInputCmdType cmd, bool pressed)
    {
        if (session?.AttachedEntity == null)
            return;

        var playerUid = session.AttachedEntity.Value;

        if (!TryComp<BuckleComponent>(playerUid, out var buckle) || buckle.BuckledTo == null)
            return;

        var buckledTo = buckle.BuckledTo.Value;

        if (!TryComp<VehicleSeatComponent>(buckledTo, out var seat) || !seat.IsDriver)
            return;

        switch (cmd)
        {
            case VehicleInputCmdType.Handbrake:
                HandleHandbrake(buckledTo, pressed);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  РУЧНОЙ ТОРМОЗ
    // ══════════════════════════════════════════════════════════════════════

    private void HandleHandbrake(EntityUid vehicleUid, bool pressed)
    {
        if (!TryComp<HandbrakeComponent>(vehicleUid, out var handbrake))
            return;

        if (!TryComp<PhysicsComponent>(vehicleUid, out var body))
            return;

        if (pressed && !handbrake.Engaged)
        {
            handbrake.Engaged = true;
            Dirty(vehicleUid, handbrake);

            // Мгновенная остановка при включении ручника
            _physics.SetLinearVelocity(vehicleUid, Vector2.Zero, body: body);
            _physics.SetAngularVelocity(vehicleUid, 0f, body: body);

            if (handbrake.EngageSound != null)
                _audio.PlayPredicted(handbrake.EngageSound, vehicleUid, null);
        }
        else if (!pressed && handbrake.Engaged)
        {
            handbrake.Engaged = false;
            Dirty(vehicleUid, handbrake);

            if (handbrake.ReleaseSound != null)
                _audio.PlayPredicted(handbrake.ReleaseSound, vehicleUid, null);
        }
    }

    private bool IsHandbrakeActive(EntityUid vehicleUid)
    {
        return TryComp<HandbrakeComponent>(vehicleUid, out var hb) && hb.Engaged;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ФАРЫ (ACTION SYSTEM)
    // ══════════════════════════════════════════════════════════════════════

    private void OnHeadlightsToggleAction(EntityUid uid, VehicleHeadlightsComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        // Only handle if this is the headlights action
        if (component.ToggleActionEntity == null || component.ToggleActionEntity.Value != args.Action.Owner)
            return;

        ToggleHeadlights(uid, component, args.Performer);
        args.Handled = true;
    }

    private void ToggleHeadlights(EntityUid vehicleUid, VehicleHeadlightsComponent? lights = null, EntityUid? user = null)
    {
        if (!Resolve(vehicleUid, ref lights, false))
            return;

        lights.Enabled = !lights.Enabled;
        Dirty(vehicleUid, lights);

        if (_light.TryGetLight(vehicleUid, out var light))
        {
            _light.SetEnabled(vehicleUid, lights.Enabled, light);
            if (lights.Enabled)
            {
                _light.SetColor(vehicleUid, lights.LightColor, light);
                _light.SetRadius(vehicleUid, lights.LightRadius, light);
                _light.SetEnergy(vehicleUid, lights.LightEnergy, light);
                _light.SetSoftness(vehicleUid, lights.LightSoftness, light);
            }
        }

        if (lights.ToggleActionEntity != null && lights.ToggleActionEntity.Value.IsValid())
            _actions.SetToggled(lights.ToggleActionEntity.Value, lights.Enabled);

        if (lights.ToggleSound != null)
            _audio.PlayPredicted(lights.ToggleSound, vehicleUid, user);
    }

    private void GrantHeadlightsAction(EntityUid driverUid, EntityUid vehicleUid)
    {
        if (!TryComp<VehicleHeadlightsComponent>(vehicleUid, out var lights))
            return;

        if (lights.ToggleActionEntity == null || !lights.ToggleActionEntity.Value.IsValid())
            _actionContainer.EnsureAction(vehicleUid, ref lights.ToggleActionEntity, lights.ToggleAction);

        if (lights.ToggleActionEntity == null || !lights.ToggleActionEntity.Value.IsValid())
            return;

        _actions.AddAction(driverUid, lights.ToggleActionEntity.Value, vehicleUid);
    }

    private void RevokeHeadlightsAction(EntityUid driverUid, EntityUid vehicleUid)
    {
        if (!TryComp<VehicleHeadlightsComponent>(vehicleUid, out var lights))
            return;

        if (lights.ToggleActionEntity != null && lights.ToggleActionEntity.Value.IsValid())
            _actions.RemoveAction(driverUid, lights.ToggleActionEntity.Value);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  РУЧНИК (ACTION SYSTEM — TOGGLE)
    // ══════════════════════════════════════════════════════════════════════

    private void OnHandbrakeToggleAction(EntityUid uid, VehicleComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        // Only handle if this is the handbrake action, not headlights
        if (TryComp<VehicleHeadlightsComponent>(uid, out var lights)
            && lights.ToggleActionEntity != null
            && lights.ToggleActionEntity.Value == args.Action.Owner)
            return;

        ToggleHandbrake(uid, component);
        args.Handled = true;
    }

    private void ToggleHandbrake(EntityUid vehicleUid, VehicleComponent? vehicle = null)
    {
        if (!Resolve(vehicleUid, ref vehicle, false))
            return;

        vehicle.HandbrakeActive = !vehicle.HandbrakeActive;
        Dirty(vehicleUid, vehicle);

        if (vehicle.HandbrakeActionEntity != null && vehicle.HandbrakeActionEntity.Value.IsValid())
            _actions.SetToggled(vehicle.HandbrakeActionEntity.Value, vehicle.HandbrakeActive);

        // Синхронизация с HandbrakeComponent (если есть)
        if (TryComp<HandbrakeComponent>(vehicleUid, out var hb))
        {
            hb.Engaged = vehicle.HandbrakeActive;
            Dirty(vehicleUid, hb);
        }

        if (vehicle.HandbrakeActive)
        {
            if (hb != null && hb.EngageSound != null)
                _audio.PlayPredicted(hb.EngageSound, vehicleUid, null);
        }
        else
        {
            if (hb != null && hb.ReleaseSound != null)
                _audio.PlayPredicted(hb.ReleaseSound, vehicleUid, null);
        }
    }

    private void GrantHandbrakeAction(EntityUid driverUid, EntityUid vehicleUid)
    {
        if (!TryComp<VehicleComponent>(vehicleUid, out var vehicle))
            return;

        if (vehicle.HandbrakeActionEntity == null || !vehicle.HandbrakeActionEntity.Value.IsValid())
            _actionContainer.EnsureAction(vehicleUid, ref vehicle.HandbrakeActionEntity, vehicle.HandbrakeAction);

        if (vehicle.HandbrakeActionEntity == null || !vehicle.HandbrakeActionEntity.Value.IsValid())
            return;

        _actions.AddAction(driverUid, vehicle.HandbrakeActionEntity.Value, vehicleUid);
    }

    private void RevokeHandbrakeAction(EntityUid driverUid, EntityUid vehicleUid)
    {
        if (!TryComp<VehicleComponent>(vehicleUid, out var vehicle))
            return;

        if (vehicle.HandbrakeActionEntity != null && vehicle.HandbrakeActionEntity.Value.IsValid())
            _actions.RemoveAction(driverUid, vehicle.HandbrakeActionEntity.Value);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ПАССАЖИРСКИЕ МЕСТА (СПАВН)
    // ══════════════════════════════════════════════════════════════════════

    private void OnVehicleStartup(EntityUid uid, VehicleComponent component, ComponentStartup args)
    {
        SpawnPassengerSeats(uid, component);
    }

    private void SpawnPassengerSeats(EntityUid vehicleUid, VehicleComponent component)
    {
        if (component.PassengerSeatEntities.Count > 0)
            return;

        var count = Math.Min(component.PassengerSeatCount, component.PassengerOffsets.Count);
        for (var i = 0; i < count; i++)
        {
            var seatUid = SpawnPassengerSeat(vehicleUid, component.PassengerOffsets[i], i);
            component.PassengerSeatEntities.Add(seatUid);
        }
        Dirty(vehicleUid, component);
    }

    private EntityUid SpawnPassengerSeat(EntityUid vehicleUid, Vector2 offset, int index)
    {
        var mapUid = Transform(vehicleUid).MapUid;
        if (mapUid == null || !mapUid.Value.IsValid())
            return EntityUid.Invalid;

        var seatUid = EntityManager.CreateEntityUninitialized(null, mapUid.Value);
        _transform.SetCoordinates(seatUid, new EntityCoordinates(vehicleUid, offset));

        EntityManager.AddComponent(seatUid, new StrapComponent());
        EntityManager.AddComponent(seatUid, new VehicleSeatComponent { IsDriver = false, SeatIndex = index + 1 });

        EntityManager.InitializeAndStartEntity(seatUid);
        _transform.SetParent(seatUid, vehicleUid);
        return seatUid;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ВХОД/ВЫХОД ИЗ ТРАНСПОРТА (INTERACT HAND + DRAGDROP + DOAFTER)
    // ══════════════════════════════════════════════════════════════════════

    private void OnInteractHand(EntityUid uid, VehicleComponent component, InteractHandEvent args)
    {
        if (args.Handled || args.User == uid)
            return;

        if (!TryComp<BuckleComponent>(args.User, out var buckle))
            return;

        // Если уже пристёгнут — отстегнуть
        if (buckle.BuckledTo != null && TryComp<StrapComponent>(buckle.BuckledTo.Value, out var strap)
            && strap.Owner == uid)
        {
            _buckle.TryUnbuckle(args.User, args.User, buckle);
            args.Handled = true;
            return;
        }

        StartEnterDoAfter(args.User, uid, component);
        args.Handled = true;
    }

    private void OnEnterDoAfter(EntityUid uid, VehicleComponent component, VehicleEnterDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryComp<BuckleComponent>(args.User, out var buckle))
            return;

        args.Handled = _buckle.TryBuckle(args.User, args.User, uid, buckle);
    }

    private void StartEnterDoAfter(EntityUid user, EntityUid vehicle, VehicleComponent component)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.EnterDelay, new VehicleEnterDoAfterEvent(), vehicle, target: vehicle)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDragDropTarget(EntityUid uid, VehicleComponent component, DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<BuckleComponent>(args.User, out var buckle))
            return;

        if (buckle.BuckledTo != null && TryComp<StrapComponent>(buckle.BuckledTo.Value, out var strap)
            && strap.Owner == uid)
        {
            _buckle.TryUnbuckle(args.User, args.User, buckle);
            args.Handled = true;
            return;
        }

        if (args.Dragged == args.User)
        {
            StartEnterDoAfter(args.User, uid, component);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.EnterDelay, new VehicleDragDoAfterEvent(), uid, target: uid, used: args.Dragged)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };
        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragDoAfter(EntityUid uid, VehicleComponent component, VehicleDragDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var dragged = args.Used ?? args.User;
        args.Handled = _buckle.TryBuckle(dragged, args.User, uid);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ПРИСТЕГИВАНИЕ / ОТСТЕГИВАНИЕ ВОДИТЕЛЯ
    // ══════════════════════════════════════════════════════════════════════

    private void OnStrapped(EntityUid uid, VehicleSeatComponent seat, ref StrappedEvent args)
    {
        if (seat.IsDriver)
            SetupDriver(args.Buckle, uid);
    }

    private void OnUnstrapped(EntityUid uid, VehicleSeatComponent seat, ref UnstrappedEvent args)
    {
        if (seat.IsDriver)
            RemoveDriver(args.Buckle, uid);
    }

    private void SetupDriver(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp))
            return;

        if (HasComp<VehicleDriverComponent>(driver))
            return;

        var driverComp = EnsureComp<VehicleDriverComponent>(driver);
        driverComp.Vehicle = vehicle;
        Dirty(driver, driverComp);

        vehicleComp.EngineRunning = true;
        Dirty(vehicle, vehicleComp);

        if (vehicleComp.StartupSound != null)
            _audio.PlayPredicted(vehicleComp.StartupSound, vehicle, driver);

        GrantHeadlightsAction(driver, vehicle);
        GrantHandbrakeAction(driver, vehicle);
    }

    private void RemoveDriver(EntityUid driver, EntityUid vehicle)
    {
        if (!TryComp<VehicleComponent>(vehicle, out var vehicleComp))
            return;

        if (!HasComp<VehicleDriverComponent>(driver))
            return;

        StopLoopSound(vehicleComp);

        if (vehicleComp.ShutoffSound != null)
            _audio.PlayPredicted(vehicleComp.ShutoffSound, vehicle, driver);

        RevokeHeadlightsAction(driver, vehicle);
        RevokeHandbrakeAction(driver, vehicle);

        RemComp<VehicleDriverComponent>(driver);
        RemComp<RelayInputMoverComponent>(driver);

        vehicleComp.EngineRunning = false;
        vehicleComp.CurrentSoundState = VehicleSoundState.None;
        Dirty(vehicle, vehicleComp);

        if (TryComp<HandbrakeComponent>(vehicle, out var hb) && hb.Engaged)
        {
            hb.Engaged = false;
            Dirty(vehicle, hb);
        }

        if (vehicleComp.HandbrakeActive)
        {
            vehicleComp.HandbrakeActive = false;
            Dirty(vehicle, vehicleComp);

            if (vehicleComp.HandbrakeActionEntity != null && vehicleComp.HandbrakeActionEntity.Value.IsValid())
                _actions.SetToggled(vehicleComp.HandbrakeActionEntity.Value, false);
        }

        if (TryComp<VehicleHeadlightsComponent>(vehicle, out var lights) && lights.Enabled)
        {
            lights.Enabled = false;
            Dirty(vehicle, lights);

            if (_light.TryGetLight(vehicle, out var light))
                _light.SetEnabled(vehicle, false, light);

            if (lights.ToggleActionEntity != null && lights.ToggleActionEntity.Value.IsValid())
                _actions.SetToggled(lights.ToggleActionEntity.Value, false);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ГЛАВНЫЙ ЦИКЛ ФИЗИКИ (КАЖДЫЙ ТИК)
    //
    //  Логика инерции:
    //  - При нажатии W/S: velocity плавно приближается к целевому через Lerp.
    //  - При отпускании: velocity умножается на (1 - friction), плавно тормозя.
    //  - Направление = вектор forward из текущего rotation транспорта.
    //  - Дрифт: при повороте добавляется боковое ускорение.
    // ══════════════════════════════════════════════════════════════════════

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var driverQuery = EntityQueryEnumerator<VehicleDriverComponent, InputMoverComponent>();
        while (driverQuery.MoveNext(out var driverUid, out var driverComp, out var mover))
        {
            if (!TryComp<VehicleComponent>(driverComp.Vehicle, out var vehicle))
                continue;
            if (!_physicsQuery.TryComp(driverComp.Vehicle, out var body))
                continue;

            var vehicleUid = driverComp.Vehicle;

            // ── Чтение ввода ───────────────────────────────────────────────
            var buttons = mover.HeldMoveButtons;
            var hasForward = (buttons & MoveButtons.Up) != 0;
            var hasBackward = (buttons & MoveButtons.Down) != 0;
            var hasLeft = (buttons & MoveButtons.Left) != 0;
            var hasRight = (buttons & MoveButtons.Right) != 0;
            var hasAnyInput = hasForward || hasBackward;
            var hasAnyTurn = hasLeft || hasRight;

            var isBraking = vehicle.HandbrakeActive;

            // ════════════════════════════════════════════════════════════════
            //  ПОВОРОТ (A/D) — плавное нарастание угловой скорости
            //
            //  Целевая угловая скорость = MaxTurnSpeed (при вводе A/D).
            //  Текущая плавно приближается через MoveTowards, затем
            //  применяется к углу транспорта.
            // ════════════════════════════════════════════════════════════════
            var targetAngularVel = hasAnyTurn
                ? (hasLeft ? vehicle.MaxTurnSpeed : -vehicle.MaxTurnSpeed)
                : 0f;

            var turnAccel = vehicle.MaxTurnSpeed * 4f; // нарастание за ~0.25 сек
            vehicle.CurrentAngularVelocity += Math.Clamp(
                targetAngularVel - vehicle.CurrentAngularVelocity,
                -turnAccel * frameTime,
                turnAccel * frameTime);

            // Обновляем угол транспорта
            var currentRot = _transform.GetWorldRotation(vehicleUid);
            _transform.SetWorldRotation(vehicleUid, currentRot + vehicle.CurrentAngularVelocity * frameTime);

            // ════════════════════════════════════════════════════════════════
            //  ЛИНЕЙНОЕ ДВИЖЕНИЕ (W/S)
            //
            //  Направление forward вычисляется из rotation транспорта.
            //  Спрайт по умолчанию смотрит "вверх" (north) при rotation=0.
            //
            //  При нажатии W: разгон вперёд.
            //  При нажатии S: если едем вперёд — тормозим, потом задний ход.
            //  Без ввода: накат / ручник.
            // ════════════════════════════════════════════════════════════════
            var velocity = body.LinearVelocity;
            var rotation = (float)_transform.GetWorldRotation(vehicleUid).Theta;

            // Вектор "вперёд" (спрайт смотрит вверх при rotation=0)
            var forward = new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation));

            // Определяем направление ввода (0, +1 или -1)
            var inputSign = hasForward ? 1f : hasBackward ? -1f : 0f;

            // Проверяем, сонаправлен ли ввод с текущим движением
            var isOpposing = false;
            if (inputSign != 0f && vehicle.CurrentSpeed > vehicle.StopThreshold)
            {
                var inputDir = forward * inputSign;
                var dot = Vector2.Dot(inputDir, velocity);
                isOpposing = dot < 0f;
            }

            if (isOpposing)
            {
                // ── Ввод против движения: ПРИНУДИТЕЛЬНОЕ торможение ──────
                // Сначала гасим скорость до нуля, потом разрешаем реверс
                var brakeDelta = vehicle.BrakeDeceleration * frameTime;
                vehicle.CurrentSpeed -= brakeDelta;

                if (vehicle.CurrentSpeed <= 0f)
                {
                    vehicle.CurrentSpeed = 0f;
                    // Теперь можно начать разгон в противоположную сторону
                }

                var newVel = forward * vehicle.CurrentSpeed;
                _physics.SetLinearVelocity(vehicleUid, newVel, body: body);
            }
            else if (hasForward)
            {
                // ── Разгон вперёд (линейный) ──────────────────────────────
                var targetSpeed = vehicle.MaxSpeed;
                var maxDelta = vehicle.Acceleration * frameTime;
                vehicle.CurrentSpeed += Math.Clamp(targetSpeed - vehicle.CurrentSpeed, -maxDelta, maxDelta);
                var newVel = forward * vehicle.CurrentSpeed;
                _physics.SetLinearVelocity(vehicleUid, newVel, body: body);
            }
            else if (hasBackward)
            {
                // ── Задний ход (только после полной остановки) ─────────────
                var targetSpeed = vehicle.ReverseSpeed;
                var maxDelta = vehicle.ReverseAcceleration * frameTime;
                vehicle.CurrentSpeed += Math.Clamp(targetSpeed - vehicle.CurrentSpeed, -maxDelta, maxDelta);
                var newVel = -forward * vehicle.CurrentSpeed;
                _physics.SetLinearVelocity(vehicleUid, newVel, body: body);
            }
            else
            {
                // ── Накат / Торможение ручником ───────────────────────────
                var deceleration = isBraking ? vehicle.BrakeDeceleration : vehicle.Deceleration;
                vehicle.CurrentSpeed -= deceleration * frameTime;

                if (vehicle.CurrentSpeed < vehicle.StopThreshold)
                    vehicle.CurrentSpeed = 0f;

                var newVel = forward * vehicle.CurrentSpeed;
                _physics.SetLinearVelocity(vehicleUid, newVel, body: body);
            }

            // ════════════════════════════════════════════════════════════════
            //  ДРИФТ — боковое скольжение с плавным гашением
            //
            //  Проектируем скорость на forward/right, гасим right компонент.
            //  При ручнике используется DriftGripBrake (сильный занос).
            // ════════════════════════════════════════════════════════════════
            ApplyDrift(vehicleUid, vehicle, body, frameTime, isBraking);

            // ── Обновление звука двигателя ─────────────────────────────────
            UpdateEngineSound(vehicleUid, vehicle, body);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ДРИФТ — боковое скольжение с плавным гашением
    //
    //  Проектируем текущую скорость на локальные оси "вперёд" и "вправо".
    //  Боковую скорость (right) гасим плавно через Lerp(current, 0, grip),
    //  чтобы машину заносило при резких поворотах на скорости.
    // ══════════════════════════════════════════════════════════════════════

    private void ApplyDrift(EntityUid vehicleUid, VehicleComponent vehicle, PhysicsComponent body, float frameTime, bool isBraking)
    {
        var speed = body.LinearVelocity.Length();
        if (speed < vehicle.StopThreshold)
            return;

        var rotation = (float)_transform.GetWorldRotation(vehicleUid).Theta;
        var forward = new Vector2(MathF.Sin(rotation), -MathF.Cos(rotation));
        var right = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation));

        // Проекция скорости на локальные оси
        var vel = body.LinearVelocity;
        var forwardSpeed = Vector2.Dot(vel, forward);
        var rightSpeed = Vector2.Dot(vel, right);

        // Плавное гашение боковой скорости
        // При ручнике — DriftGripBrake (сильный занос), иначе — DriftGrip
        var grip = isBraking ? vehicle.DriftGripBrake : vehicle.DriftGrip;
        var gripFactor = MathF.Min(grip * frameTime, 1f);
        rightSpeed = rightSpeed * (1f - gripFactor);

        // Собираем новую скорость из локальных компонентов
        var newVel = forward * forwardSpeed + right * rightSpeed;
        _physics.SetLinearVelocity(vehicleUid, newVel, body: body);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ЗВУКИ ДВИГАТЕЛЯ
    // ══════════════════════════════════════════════════════════════════════

    private void UpdateEngineSound(EntityUid vehicleUid, VehicleComponent vehicle, PhysicsComponent body)
    {
        var isMoving = body.LinearVelocity.LengthSquared() > 0.01f;
        var targetState = isMoving ? VehicleSoundState.Gaz : VehicleSoundState.Idle;

        if (vehicle.CurrentSoundState == targetState)
            return;

        StopLoopSound(vehicle);
        vehicle.CurrentSoundState = targetState;

        SoundSpecifier? sound = targetState switch
        {
            VehicleSoundState.Idle => vehicle.IdleSound,
            VehicleSoundState.Gaz => vehicle.GazSound,
            _ => null,
        };

        if (sound == null)
            return;

        var audioParams = AudioParams.Default.WithLoop(true).WithVolume(-5f);
        var result = _audio.PlayPredicted(sound, vehicleUid, null, audioParams);
        if (result != null)
            vehicle.LoopSoundEntity = result.Value.Entity;

        Dirty(vehicleUid, vehicle);
    }

    private void StopLoopSound(VehicleComponent vehicle)
    {
        if (vehicle.LoopSoundEntity == null || !vehicle.LoopSoundEntity.Value.IsValid())
        {
            vehicle.LoopSoundEntity = null;
            return;
        }

        _audio.Stop(vehicle.LoopSoundEntity.Value);
        vehicle.LoopSoundEntity = null;
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  ВСПОМОГАТЕЛЬНЫЕ ТИПЫ
// ══════════════════════════════════════════════════════════════════════════

[Serializable, NetSerializable]
public sealed partial class VehicleEnterDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class VehicleDragDoAfterEvent : SimpleDoAfterEvent { }

public enum VehicleInputCmdType : byte
{
    Handbrake,
}

internal sealed class VehicleInputCmdHandler : InputCmdHandler
{
    private readonly VehicleSystem _system;
    private readonly VehicleInputCmdType _cmd;

    public VehicleInputCmdHandler(VehicleSystem system, VehicleInputCmdType cmd)
    {
        _system = system;
        _cmd = cmd;
    }

    public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
    {
        _system.HandleVehicleInput(session, _cmd, message.State == BoundKeyState.Down);
        return false;
    }
}
