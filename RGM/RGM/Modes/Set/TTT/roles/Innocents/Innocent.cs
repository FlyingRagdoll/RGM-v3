using Exiled.Events.EventArgs.Player;
using Exiled.API.Features;
using static RGM.Modes.TTT;
using static RGM.Modes.TttPublics;

namespace RGM.Modes.TTTRoles;
public class  Innocent : TttRole
{
    public override string Name => "무죄인";
    public override string Description => 
        $"당신은 <color={Instance._teamColors[TttRoleType.Innocent]}>무죄인</color>입니다. <color={Instance._teamColors[TttRoleType.Detective]}>탐정</color>과 함께 <color={Instance._teamColors[TttRoleType.Traitor]}>배신자</color>들을 처단하세요.";
    public override TttTeam Team => TttTeam.Innocent;
    public override TttRoleType RoleType => TttRoleType.Innocent;
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
    }
    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (Round.IsEnded || !Instance._isTTTStarted) return;

        if (ev.Player == Owner)
        {
            ev.Player.RankName = $"{Name}";
            ev.Player.RankColor = $"{Instance._teamRankColors[RoleType]}";
            return;
        }
        if (ev.Attacker == null || ev.Attacker != Owner || ev.Attacker == ev.Player) return;
        if (!Instance._playerRoles.TryGetValue(ev.Player, out var playerRole)) return;
        if (playerRole.Team != TttTeam.Innocent) return;
        if (playerRole.RoleType == TttRoleType.Detective) return;
        Owner.Hurt(50, "같은 무죄인을 죽이는 실수를 범해서는 안됐습니다.");
    }
}
