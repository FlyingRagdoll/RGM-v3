using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using HintServiceMeow.Core.Extension;
using MEC;
using System.Collections.Generic;
using UnityEngine;
using static RGM.Modes.TTT;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes.TTTRoles;

public class Soulmate : TttRole
{
    public override string Name => "소울메이트";
    public override string Description =>
         $"당신은 <color={Instance._teamColors[TttRoleType.Soulmate]}>소울메이트</color>입니다. 당신의 짝이 어디있는지 실시간으로 확인할 수 있습니다.";
    public override TttTeam Team => TttTeam.Innocent;
    public override TttRoleType RoleType => TttRoleType.Soulmate;
    public Player mate;
    private CoroutineHandle _handle;
    public override void OnEnabled()
    {
        Exiled.Events.Handlers.Player.Died += OnDied;
        Exiled.Events.Handlers.Player.Hurting += Hurting;
        _handle = Timing.RunCoroutine(FindLocate());
    }
    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.Died -= OnDied;
        Exiled.Events.Handlers.Player.Hurting -= Hurting;
        Timing.KillCoroutines(_handle);
    }
    private void OnDied(DiedEventArgs ev)
    {
        if (ev.Player != Owner) return;
        ev.Player.RankName = $"{Name}";
        ev.Player.RankColor = Instance._teamColors[RoleType];
    }
    private void Hurting(HurtingEventArgs ev) 
    {
        if (Round.IsEnded || !Instance._isTTTStarted) return;
        if (ev.Attacker == Owner && ev.Player == mate) ev.IsAllowed = false;
    }
    private IEnumerator<float> FindLocate()
    {
        while (Owner.IsAlive) 
        {
            if (mate != null && mate.IsAlive)
            {
                Owner.AddHint("TTT 소울메이트", $"당신의 짝은 {mate.DisplayNickname}({(int)Vector3.Distance(Owner.Position, mate.Position)})m입니다.", 1.05f);
            }
            else if (mate != null && !mate.IsAlive)
            {
                Owner.AddHint("TTT 소울메이트", $"당신의 짝은 사망했습니다!", 1.05f);
            }
            yield return Timing.WaitForSeconds(1f); 
        }
    }
}
