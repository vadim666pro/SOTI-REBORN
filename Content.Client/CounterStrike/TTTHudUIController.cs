using Content.Client.Gameplay;
using Content.Shared.CounterStrike.Events;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.CounterStrike;

public sealed class TTTHudUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    private TTTHud? _hud;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TTTHudEvent>(OnHudEvent);
        SubscribeNetworkEvent<TTTHudClearEvent>(OnHudClear);
    }

    public void OnStateEntered(GameplayState state)
    {
        _hud = new TTTHud();
        UIManager.RootControl.AddChild(_hud);
        _hud.Clear();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_hud == null)
            return;

        UIManager.RootControl.RemoveChild(_hud);
        _hud.Dispose();
        _hud = null;
    }

    private void OnHudEvent(TTTHudEvent ev, EntitySessionEventArgs args)
    {
        _hud?.SetData(ev.TimeUntilPolice, ev.PoliceArrived);
    }

    private void OnHudClear(TTTHudClearEvent ev, EntitySessionEventArgs args)
    {
        _hud?.Clear();
    }
}
