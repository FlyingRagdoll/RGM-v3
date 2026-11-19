using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using RGM.API.DataBases;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RGM.Modes.TTT;

namespace RGM.Modes;

public abstract class TttRole
{
    public virtual void OnEnabled() { }
    public virtual void OnDisabled() { }
    public abstract string Name { get;}
    public abstract string Description { get;}
    public abstract TttTeam Team { get; }
    public abstract TttRoleType RoleType { get; }
    public Player Owner { get; set; }
}

public enum TttTeam { Innocent, Traitor, Neutral }
public enum  TttRoleType
{
    //무죄인
    Innocent,
    Detective,
    Soulmate,


    //배신자
    Traitor,

    
    //중립
    Jester,
    O5
}

//공용 이벤트 및 코루틴
public static class TttPublics 
{
    public static IEnumerator<float> TraitorFindLocate(Player Owner)
    {
        while (!Round.IsEnded)
        {
            if (Owner.TryGetLookPlayer(30, out Player t, out RaycastHit? hit) && Instance._playerRoles.TryGetValue(t, out var targetRole))
            {
                var text = targetRole.RoleType switch
                {
                    TttRoleType.Traitor => $"그는 당신의 동료, 같은 <color={Instance._teamColors[TttRoleType.Traitor]}>배신자</color>입니다.",
                    TttRoleType.Detective => $"그는 <color={Instance._teamColors[TttRoleType.Detective]}>탐정</color>입니다.",
                    TttRoleType.O5 => $"그는 <color={Instance._teamColors[TttRoleType.O5]}>O5</color>입니다.",
                    TttRoleType.Jester => $"그는 <color={Instance._teamColors[TttRoleType.Jester]}>광대</color>입니다.",
                    _ => $"[ALT]ㅣ해당 <color={Instance._teamColors[TttRoleType.Innocent]}>무죄인</color>을 일격에 즉사시키십시오."
                };
                Owner.AddHint("TTT 상대 확인", text, 1.05f);
            }
            else if (Owner.TryGetNearestPlayer(out Player nearestPlayer, out float radius, PlayerManager.List.Where(p => Instance._playerRoles.TryGetValue(p, out var role) && role.Team == TttTeam.Traitor).ToList()))
                Owner.AddHint("TTT 생존자 거리 확인", $"<b>[ <color={nearestPlayer.Role.Color.ToHex()}>{Trans.Role[nearestPlayer.Role.Type]}</color>, 거리: {radius:F1}m ]</b>", 1.05f);
            else Owner.AddHint("TTT 배신자 임무 완수", "당신은 임무를 완수하였습니다.", 1.05f);
            yield return Timing.WaitForSeconds(1);
        }
    }
}
/*
직업을 생성할 때
1. RankName과 RankColor을 설정하는 것이 필요합니다.(조건에 따라 이벤트에 알아서 하심 됩니다)
2. 직업을 추가했을 때 필요하면 TraitorFIndLocate와 관련한 메서드, 이벤트를 수정하세요.
3. Attribute를 사용하지 않기 때문에 TTT.cs에 List에 직업의 클래스를 추가해야 합니다.
4. _teamRankColors, _teamColors에 직업에 대한 색을 추가하세요.
5. OnDied에 
    if (Round.IsEnded || !Instance._isTTTStarted) return;
   이게 있으면 좋습니다.
*/