using System.Collections.Generic;
using Exiled.API.Features.Items;
using Exiled.API.Features.Roles;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Mythic;

/*[Ability("무제한", "제한이 사라집니다. (무제한 모드와 동일)",
    AbilityCategory.Mythic, AbilityType.MYTHIC_UNLIMITED, RoleAbility.None, true)]*/
public class Unlimited : Ability
{
    private int _tantrum;
    private CoroutineHandle _onStarted;

    public override void OnEnabled()
    {
        Owner.IsUsingStamina = false;

        if (Owner.Role.Type == RoleTypeId.Scp0492)
            Owner.MaxHealth += 100;

        Exiled.Events.Handlers.Player.Healing += OnHealing;
        Exiled.Events.Handlers.Player.UsingItem += OnUsingItem;
        Exiled.Events.Handlers.Player.UsingRadioBattery += OnUsingRadioBattery;

        Exiled.Events.Handlers.Scp106.Teleporting += OnTeleporting;
        Exiled.Events.Handlers.Scp106.Stalking += OnStalking;
        Exiled.Events.Handlers.Scp106.Attacking += OnScp106Attacking;

        Exiled.Events.Handlers.Scp939.PlayingSound += OnPlayingSound;

        Exiled.Events.Handlers.Scp079.ChangingCamera += OnChangingCamera;
        Exiled.Events.Handlers.Scp079.Pinging += OnPinging;

        Exiled.Events.Handlers.Scp049.StartingRecall += OnStartingRecall;
        Exiled.Events.Handlers.Scp049.Attacking += OnScp049Attacking;

        Exiled.Events.Handlers.Scp096.Enraging += OnEnraging;

        Exiled.Events.Handlers.Scp173.PlacingTantrum += OnPlacingTantrum;
        Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds += OnUsingBreakneckSpeeds;

        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState += OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy += OnUsingMicroHIDEnergy;

        Exiled.Events.Handlers.Item.ChargingJailbird += OnChargingJailbird;

        _onStarted = Timing.RunCoroutine(OnStarted());
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Healing -= OnHealing;
        Exiled.Events.Handlers.Player.UsingItem -= OnUsingItem;
        Exiled.Events.Handlers.Player.UsingRadioBattery -= OnUsingRadioBattery;

        Exiled.Events.Handlers.Scp106.Teleporting -= OnTeleporting;
        Exiled.Events.Handlers.Scp106.Stalking -= OnStalking;
        Exiled.Events.Handlers.Scp106.Attacking -= OnScp106Attacking;

        Exiled.Events.Handlers.Scp939.PlayingSound -= OnPlayingSound;

        Exiled.Events.Handlers.Scp079.ChangingCamera -= OnChangingCamera;
        Exiled.Events.Handlers.Scp079.Pinging -= OnPinging;


        Exiled.Events.Handlers.Scp049.StartingRecall -= OnStartingRecall;
        Exiled.Events.Handlers.Scp049.Attacking -= OnScp049Attacking;

        Exiled.Events.Handlers.Scp096.Enraging -= OnEnraging;

        Exiled.Events.Handlers.Scp173.PlacingTantrum -= OnPlacingTantrum;
        Exiled.Events.Handlers.Scp173.UsingBreakneckSpeeds -= OnUsingBreakneckSpeeds;

        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.ChangingMicroHIDState -= OnChangingMicroHIDState;
        Exiled.Events.Handlers.Player.UsingMicroHIDEnergy -= OnUsingMicroHIDEnergy;

        Exiled.Events.Handlers.Item.ChargingJailbird -= OnChargingJailbird;

        Timing.KillCoroutines(_onStarted);
    }

    private IEnumerator<float> OnStarted()
    {
        while (true)
        {
            switch (Owner.Role)
            {
                case Scp049Role scp049:
                    scp049.CallCooldown = 0;
                    scp049.GoodSenseCooldown = 0;
                    scp049.RemainingAttackCooldown = 0;
                    break;
                case Scp106Role scp106:
                    scp106.CaptureCooldown = 0;
                    scp106.RemainingSinkholeCooldown = 0;
                    break;
                case Scp173Role scp173:
                    scp173.BlinkCooldown = 0.5f;
                    scp173.RemainingBreakneckCooldown = 0.5f;
                    break;
                case Scp096Role scp096:
                    scp096.EnrageCooldown = 0;
                    scp096.ChargeCooldown = 0;
                    break;
                case Scp939Role scp939:
                    scp939.MimicryCooldown = 0;
                    scp939.AmnesticCloudCooldown = 0;
                    scp939.AttackCooldown = 0;
                    break;
                case Scp079Role scp079:
                {
                    scp079.BlackoutZoneCooldown = 0;
                    scp079.RoomLockdownCooldown = 0;

                    if (scp079.PingAbility != null)
                    {
                        var field = typeof(PlayerRoles.PlayableScps.Scp079.Pinging.Scp079PingAbility)
                            .GetField("_rateLimiter", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

                        if (field != null)
                        {
                            field.SetValue(scp079.PingAbility, new RateLimiter(0f));
                        }
                    }

                    break;
                }
                case Scp0492Role scp0492:
                case Scp3114Role scp3114:
                    break;
            }

            yield return Timing.WaitForSeconds(1f);
        }
    }


    private void OnHealing(HealingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Player.MaxHealth += ev.Amount;
    }

    private IEnumerator<float> OnUsingItem(UsingItemEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Cooldown = 0;
    }

    private void OnUsingRadioBattery(UsingRadioBatteryEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Drain = 0;
    }

    private IEnumerator<float> OnTeleporting(Exiled.Events.EventArgs.Scp106.TeleportingEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp106.RemainingSinkholeCooldown = 0;
    }

    private IEnumerator<float> OnStalking(Exiled.Events.EventArgs.Scp106.StalkingEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp106.RemainingSinkholeCooldown = 0;
    }

    private IEnumerator<float> OnScp106Attacking(Exiled.Events.EventArgs.Scp106.AttackingEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp106.CaptureCooldown = 0;
    }

    private IEnumerator<float> OnPlayingSound(Exiled.Events.EventArgs.Scp939.PlayingSoundEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp939.MimicryCooldown = 0;
    }

    private void OnChangingCamera(Exiled.Events.EventArgs.Scp079.ChangingCameraEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Scp079.Energy = 100000;
    }

    private void OnPinging(Exiled.Events.EventArgs.Scp079.PingingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;
        ev.Scp079.Energy = 100000;
    }

    private IEnumerator<float> OnStartingRecall(Exiled.Events.EventArgs.Scp049.StartingRecallEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp049.RemainingCallDuration = 0;
    }

    private IEnumerator<float> OnScp049Attacking(Exiled.Events.EventArgs.Scp049.AttackingEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp049.RemainingAttackCooldown = 0;
        ev.Scp049.RemainingGoodSenseDuration = 0;
    }

    private IEnumerator<float> OnEnraging(Exiled.Events.EventArgs.Scp096.EnragingEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForOneFrame;

        ev.Scp096.EnrageCooldown = 0;
        ev.Scp096.EnragedTimeLeft = 99999;
        ev.Scp096.SprintingSpeed = 500;
    }

    private IEnumerator<float> OnPlacingTantrum(Exiled.Events.EventArgs.Scp173.PlacingTantrumEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        if (_tantrum >= 10)
        {
            ev.Player.AddHint("무제한 땅콩 똥 제한", $"렉 방지를 위해 10개로 제한됩니다. (하나 당 180초)", 1);
            ev.IsAllowed = false;
        }
        else
        {
            _tantrum += 1;

            yield return Timing.WaitForOneFrame;

            ev.Cooldown.Remaining = 0;

            yield return Timing.WaitForSeconds(180f);

            _tantrum -= 1;
        }
    }

    private IEnumerator<float> OnUsingBreakneckSpeeds(Exiled.Events.EventArgs.Scp173.UsingBreakneckSpeedsEventArgs ev)
    {
        if (ev.Player != Owner)
            yield break;

        yield return Timing.WaitForSeconds(0.5f);

        ev.Scp173.RemainingBreakneckCooldown = 0;
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Player.CurrentItem.As<Firearm>().MagazineAmmo = 250;
    }

    private void OnChangingMicroHIDState(ChangingMicroHIDStateEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.MicroHID.Energy += 100;
    }

    private void OnUsingMicroHIDEnergy(UsingMicroHIDEnergyEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.MicroHID.Energy += 100;
    }

    private void OnChargingJailbird(Exiled.Events.EventArgs.Item.ChargingJailbirdEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        ev.Jailbird.TotalCharges = 0;
        ev.Jailbird.TotalDamageDealt = 0;
    }
}
