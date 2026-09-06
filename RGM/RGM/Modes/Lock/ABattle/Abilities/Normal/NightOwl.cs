using Exiled.API.Enums;
using RGM.API.Features;

namespace RGM.Modes.Abilities.Normal;

[Ability("밤눈", "NightVision 효과를 5p 획득합니다.", AbilityCategory.Normal, AbilityType.NORMAL_NIGHTOWL)]
public class NightOwl : Ability
{
    public override void OnEnabled()
    {
        Owner.AddEffect(EffectType.NightVision, 5);
    }
}