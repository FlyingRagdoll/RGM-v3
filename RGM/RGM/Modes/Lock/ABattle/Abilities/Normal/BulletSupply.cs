using Exiled.Events.EventArgs.Player;
using UnityEngine;

namespace RGM.Modes.Abilities.Normal;

//[Ability("잔탄 수급", "총알을 발사했을 때, 아무도 맞지 않았다면 20% 확률로 해당 총기에 총알이 지급됩니다.", AbilityCategory.Normal, AbilityType.NORMAL_BULLETSUPPLY)]
public class BulletSupply : Ability
{
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
    }

    private void OnShooting(ShootingEventArgs ev)
    {
        if (ev.Player != Owner)
            return;

        if (ev.ClaimedTarget != null) return;
        if (Random.Range(1, 6) == 1)
            ev.Firearm.MagazineAmmo += 1;
    }
}
