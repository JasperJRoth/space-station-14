﻿using Content.Server.Alert;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Shared.Alert;
using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
{
    public class DeadMobState : SharedDeadMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out ServerAlertsComponent? status))
            {
                status.ShowAlert(AlertType.HumanDead);
            }

            if (entity.TryGetComponent(out StunnableComponent? stun))
            {
                // TODO: Use resolves to pass ServerAlertsComponent here.
                EntitySystem.Get<StunSystem>().Reset(entity.Uid, stun);
            }
        }
    }
}
