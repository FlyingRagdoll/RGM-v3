using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Rare;

//[Ability("스테로이드", "25초 간 이동 속도가 많이 증가합니다.", AbilityCategory.Rare, AbilityType.RARE_STEROID)]
public class Steroid : Ability
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.MovementBoost, 50, 25);
    }
}
