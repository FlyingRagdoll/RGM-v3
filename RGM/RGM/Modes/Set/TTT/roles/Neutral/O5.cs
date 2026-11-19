using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.Modes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RGM.Modes.TTT;

namespace RGM.Modes.TTTRoles;

public class O5 : TttRole
{
    public override string Name => "O5 평의회";
    public override string Description =>
        $"당신은 <color={TTT.Instance._teamColors[TttRoleType.O5]}>O5 평의회</color>입니다. 끝까지 생존하거나, 나머지를 전부 죽이세요!";
    public override TttTeam Team => TttTeam.Neutral;
    public override TttRoleType RoleType => TttRoleType.O5;

    public override void OnEnabled()
    {
        Owner.MaxHealth = 240;
        Owner.Health = Owner.MaxHealth;
        Owner.AddItem(ItemType.Painkillers);
        Exiled.Events.Handlers.Player.Died += OnDied;
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }
    private void OnDied(DiedEventArgs ev)
    {
        Owner.RankName = $"{Name}";
        Owner.RankColor = Instance._teamRankColors[RoleType];
    }
}
