using Content.Shared.GameTicking;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.GameTicking
{
    public sealed class RoleRevealSystem : EntitySystem
    {
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private RoleRevealOverlay? _currentOverlay;
        private TimeSpan _expire = TimeSpan.Zero;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<RoleRevealEvent>(OnRoleRevealEvent);
        }

        private void OnRoleRevealEvent(RoleRevealEvent ev, EntitySessionEventArgs args)
        {
            // Create overlay and add it
            _currentOverlay = new RoleRevealOverlay();
            _currentOverlay.Start(ev.ImagePath, ev.RoleName, ev.AntagName, ev.DisplayTime, ev.FadeTime);
            _overlayManager.AddOverlay(_currentOverlay);
            _expire = _timing.CurTime + TimeSpan.FromSeconds(ev.DisplayTime + ev.FadeTime + 0.1);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (_currentOverlay == null)
                return;

            if (_timing.CurTime >= _expire)
            {
                _overlayManager.RemoveOverlay<RoleRevealOverlay>();
                _currentOverlay = null;
            }
        }
    }
}
