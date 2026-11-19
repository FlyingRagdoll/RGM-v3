using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;
using PlayerRoles;
using RGM.Modes;
using RGM.API.Features;
using System;
using InventorySystem.Items.Usables.Scp330;
using Exiled.API.Extensions;
using static RGM.Modes.TTT;
using Random = UnityEngine.Random;

namespace RGM.Modes.TTTRoles;

public class Jester : TttRole
{
    public override string Name => "광대";
    public override string Description =>
        $"당신은 <color={TTT.Instance._teamColors[TttRoleType.Jester]}>광대</color>입니다. 사망하기 전까지 공격할 수 없으며,\n<color={TTT.Instance._teamColors[TttRoleType.Innocent]}>무죄인</color>에게 사망할 경우 1번 부활하고, 모두가 당신의 정체에 대해 알게 됩니다.";
    public override TttTeam Team => TttTeam.Neutral;
    public override TttRoleType RoleType => TttRoleType.Jester;

    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Shooting += OnShooting;
        Exiled.Events.Handlers.Player.Died += OnDied;
    }
    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Shooting -= OnShooting;
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }
    private void OnShooting(ShootingEventArgs ev) 
    {
        if (ev.Player != Owner) return;
        if (Owner.Role == RoleTypeId.Scientist) return;
        ev.IsAllowed = false;

    }
    private void OnDied(DiedEventArgs ev)
    {
        if (Round.IsEnded || !Instance._isTTTStarted) return;
        if (ev.Player != Owner) return;
        Owner.RankName = $"{Name}";
        Owner.RankColor = Instance._teamRankColors[RoleType];
        if (Owner.Role == RoleTypeId.Scientist) return;
        if (ev.Attacker == null || !Instance._playerRoles.TryGetValue(ev.Attacker, out var attackerRole)) return;
        if (attackerRole.Team == TttTeam.Traitor || attackerRole.Team == TttTeam.Neutral) return; //만일 팀이 아니라 직업으로 구분하고 싶으면 제외 List<TttRoleType>하고 거르면 됩니다

        Timing.CallDelayed(3, () =>
        {
            ev.Player.Role.Set(RoleTypeId.Scientist, RoleSpawnFlags.None);
            ev.Player.AddItem(Instance._tttprimary.GetRandomValue());
            ev.Player.AddItem(Instance._tttsecondary.GetRandomValue());
            ev.Player.Position = Tools.GetObjectList("Spot Random").GetRandomValue().position;
            switch (Convert.ToByte(Random.Range(1, 101)))
            {
                case 1:
                    ev.Player.AddCandy(CandyKindID.Pink);
                    break;

                case >= 2 and <= 16:
                    ev.Player.AddItem(ItemType.GrenadeHE);
                    break;

                case >= 17 and <= 33:
                    ev.Player.AddItem(ItemType.Painkillers);
                    break;

                case >= 34 and <= 50:
                    ev.Player.AddItem(ItemType.Medkit);
                    break;
            }
        });
    }
}
