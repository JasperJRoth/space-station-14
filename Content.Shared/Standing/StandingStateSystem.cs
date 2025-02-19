using System;
using Content.Shared.Audio;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Rotation;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Content.Shared.Standing
{
    public sealed class StandingStateSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;

        [Obsolete("Use the EntityUid overloads instead.")]
        public bool IsDown(IEntity entity)
        {
            return IsDown(entity.Uid);
        }

        public bool IsDown(EntityUid uid, StandingStateComponent? standingState = null)
        {
            if (!Resolve(uid, ref standingState, false))
                return false;

            return !standingState.Standing;
        }

        [Obsolete("Use the EntityUid overloads instead.")]
        public void Down(IEntity entity, bool playSound = true, bool dropHeldItems = true)
        {
            Down(entity.Uid, playSound, dropHeldItems);
        }

        public bool Down(EntityUid uid, bool playSound = true, bool dropHeldItems = true,
            StandingStateComponent? standingState = null,
            SharedAppearanceComponent? appearance = null,
            SharedHandsComponent? hands = null)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, ref hands, false);

            if (!standingState.Standing)
                return true;

            // This is just to avoid most callers doing this manually saving boilerplate
            // 99% of the time you'll want to drop items but in some scenarios (e.g. buckling) you don't want to.
            // We do this BEFORE downing because something like buckle may be blocking downing but we want to drop hand items anyway
            // and ultimately this is just to avoid boilerplate in Down callers + keep their behavior consistent.
            if (dropHeldItems && hands != null)
            {
                _sharedHandsSystem.DropHandItems(uid, false, hands);
            }

            var msg = new DownAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;

            standingState.Standing = false;
            standingState.Dirty();
            RaiseLocalEvent(uid, new DownedEvent(), false);

            // Seemed like the best place to put it
            appearance?.SetData(RotationVisuals.RotationState, RotationState.Horizontal);

            // Currently shit is only downed by server but when it's predicted we can probably only play this on server / client
            if (playSound)
            {
                SoundSystem.Play(Filter.Pvs(uid), standingState.DownSoundCollection.GetSound(), uid, AudioHelpers.WithVariation(0.25f));
            }

            return true;
        }

        [Obsolete("Use the EntityUid overloads instead.")]
        public void Stand(IEntity entity)
        {
            Stand(entity.Uid);
        }

        public bool Stand(EntityUid uid,
            StandingStateComponent? standingState = null,
            SharedAppearanceComponent? appearance = null)
        {
            // TODO: This should actually log missing comps...
            if (!Resolve(uid, ref standingState, false))
                return false;

            // Optional component.
            Resolve(uid, ref appearance, false);

            if (standingState.Standing)
                return true;

            var msg = new StandAttemptEvent();
            RaiseLocalEvent(uid, msg, false);

            if (msg.Cancelled)
                return false;

            standingState.Standing = true;
            standingState.Dirty();
            RaiseLocalEvent(uid, new StoodEvent(), false);

            appearance?.SetData(RotationVisuals.RotationState, RotationState.Vertical);
            return true;
        }
    }

    /// <summary>
    /// Subscribe if you can potentially block a down attempt.
    /// </summary>
    public sealed class DownAttemptEvent : CancellableEntityEventArgs
    {

    }

    /// <summary>
    /// Subscribe if you can potentially block a stand attempt.
    /// </summary>
    public sealed class StandAttemptEvent : CancellableEntityEventArgs
    {

    }

    /// <summary>
    /// Raised when an entity becomes standing
    /// </summary>
    public sealed class StoodEvent : EntityEventArgs
    {
    }

    /// <summary>
    /// Raised when an entity is not standing
    /// </summary>
    public sealed class DownedEvent : EntityEventArgs
    {
    }
}
