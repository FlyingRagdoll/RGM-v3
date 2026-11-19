using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using RGM.API.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RGM.Modes.TTT;
using static RGM.Variables.Variable;
using static RGM.Modes.TttPublics;

namespace RGM.Modes.TTTRoles;

public class Traitor : TttRole
{
    public override string Name => "배신자";
    public override string Description =>
        $"""
        당신은 <color={Instance._teamColors[TttRoleType.Traitor]}>배신자</color>입니다. <color={Instance._teamColors[TttRoleType.Traitor]}>배신자</color>들을 제외한 나머지를 모두 사살하세요.
        <size=25>{Instance.GetTraitorCount(PlayerManager.List.Count)}명의 <color={Instance._teamColors[TttRoleType.Traitor]}>배신자</color>가 존재합니다.
        <b>[ALT]ㅣ근접한 플레이어를 즉시 처형할 수 있습니다. (쿨다운 10초)</b></size>
        """;
    public override TttTeam Team => TttTeam.Traitor;
    public override TttRoleType RoleType => TttRoleType.Traitor;

    bool _isInstantKillOnCooldown = false;
    CoroutineHandle _handle;
    public override void OnEnabled()
    {
        _handle = Timing.RunCoroutine(TraitorFindLocate(Owner));
        Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
        Exiled.Events.Handlers.Player.Died += OnDied;
    }
    public override void OnDisabled()
    {
        Timing.KillCoroutines(_handle);
        Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnTogglingNoClip(TogglingNoClipEventArgs ev) 
    {
        if (ev.Player != Owner) return;
        if (_isInstantKillOnCooldown) return;
        if (!ev.Player.TryGetLookPlayer(3, out Player t, out RaycastHit? hit)) return;
        if (!Instance._playerRoles.TryGetValue(t, out var targetRole) || targetRole.Team != TttTeam.Traitor) return;
        if (GodModePlayers.Contains(t))
            GodModePlayers.Remove(t);

        t.Hit(ev.Player, ev.Player.MaxHealth);
        ev.Player.ShowHitMarker();
        _isInstantKillOnCooldown = true;

        Timing.CallDelayed(10, () => _isInstantKillOnCooldown = false);
    }

    private void OnDied(DiedEventArgs ev)
    {
        if (Round.IsEnded || !Instance._isTTTStarted) return;
        if (ev.Player != Owner) return;
        ev.Player.RankName = $"{Name}";
        ev.Player.RankColor = $"{Instance._teamRankColors[RoleType]}";
    }
}