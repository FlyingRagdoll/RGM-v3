using PlayerRoles;
using System.Collections.Generic;

namespace RGM.Modes.TTTRoles;

public class Detective : TttRole
{
    public override string Name => "탐정";
    public override string Description =>
        $"당신은 <color={TTT.Instance._teamColors[TttRoleType.Detective]}>탐정</color>입니다. <color={TTT.Instance._teamColors[TttRoleType.Traitor]}>배신자</color>들을 처단하세요.";
    public override TttTeam Team => TttTeam.Innocent;
    public override TttRoleType RoleType => TttRoleType.Detective;
    public override void OnEnabled()
    {
        Owner.Role.Set(RoleTypeId.FacilityGuard, RoleSpawnFlags.None);
        Owner.RankName = $"{Name}";
        Owner.RankColor = TTT.Instance._teamRankColors[RoleType];

        foreach (var item in new List<ItemType>
            {
                ItemType.ArmorCombat,
                ItemType.Painkillers,
                ItemType.Adrenaline,
                ItemType.Medkit
            })
        {
            Owner.AddItem(item);
        }
    }
    public override void OnDisabled()
    {
        Owner.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.None);
    }
    
}
