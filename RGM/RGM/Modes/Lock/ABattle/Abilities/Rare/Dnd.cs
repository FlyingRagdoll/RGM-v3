using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using MEC;

namespace RGM.Modes.Abilities.Rare;

/*[Ability("자리 비움", """
                  100초 동안 움직일 수 없고 아이템을 들 수 없습니다. 
                  지속시간 이후 <color=#A4A4A4>일반</color>, <color=#2ECCFA>희귀</color>, <color=#FF00FF>영웅</color> 능력을 하나씩 획득합니다.
                  """, AbilityCategory.Rare, AbilityType.RARE_DND)]*/
public class Dnd : Ability
{
    public override void OnEnabled()
    {
        Timing.RunCoroutine(Enumerator());
        return;

        IEnumerator<float> Enumerator()
        {
            Owner.EnableEffect(EffectType.Ensnared, 1, 100);

            for (int i = 0; i < 100; i++)
            {
                if (Owner.IsDead)
                    yield break;

                Owner.CurrentItem = null;

                yield return Timing.WaitForSeconds(1f);
            }

            if (!Owner.IsDead)
            {
                List<AbilityCategory> categories = new List<AbilityCategory>
                {
                    AbilityCategory.Normal,
                    AbilityCategory.Rare,
                    AbilityCategory.Epic
                };

                foreach (var category in categories)
                {
                    try
                    {
                        Owner.AddAbility(ABattle.Instance.GetRandomAbilities(Owner, category, 3).ToList().Where(x => x != AbilityType.RARE_DND).GetRandomValue());
                        Timing.CallDelayed(1, () =>
                        {
                            Owner.RemoveAbility(this);
                        });
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Error while adding ability to {Owner.Nickname} ({Owner.UserId}): {e}");
                    }
                }

                Owner.AddAbility(AbilityType.DUMMY_NOAFK);
            }
            
        }
    }
}
