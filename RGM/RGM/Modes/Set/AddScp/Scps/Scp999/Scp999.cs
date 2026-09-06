using System;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using PlayerRoles;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using RGM.API.Components;
using RGM.API.Features;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RGM.Modes.Sets.AddScp.Scps
{
    public static class Scp999
    {
        static bool cuteCooldown = false;

        public static Player Create(Player player)
        {
            player.Role.Set(RoleTypeId.Tutorial, RoleSpawnFlags.AssignInventory);
            player.EnableEffect(EffectType.Ghostly, 1);
            player.EnableEffect(EffectType.SilentWalk, 10);
            player.EnableEffect(EffectType.Asphyxiated, 1);
            player.MaxHealth = 999;
            player.Health = player.MaxHealth;
            Timing.CallDelayed(1, () =>
            {
                player.Scale = new Vector3(0.3f, 0.3f, 0.3f);
            });
            player.AddHint("SCP-999 설명", """        
                                         <size=25>
                                         당신은 <color=red>SCP-999</color>(<color=#a4fc16>Safe</color>)입니다.
                                         </size>
                                         <size=20>
                                         모두에게 중립이며, 그들에게 버프를 줄 수 있습니다.
                                         • 플레이어를 바라보면, 속도가 증가합니다.
                                         • 플레이어 근처에 가면, 체력을 회복시킵니다.
                                         • 종종 [ALT]키를 눌러 애교를 부릴 수 있습니다.
                                         • 해당 애교는 1% 확률로 적을 심장마비로 사망시킬 수 있습니다!
                                         • 아이템을 들 수는 있으나, 무기를 쥘 수 없습니다.
                                         </size>
                                         """, 20);
                
            SchematicObject schematic = ObjectSpawner.SpawnSchematic("SCP_999", new Vector3(player.Position.x, player.Position.y - 0.1f, player.Position.z), player.Rotation, new Vector3(3, 3, 3));
            schematic.transform.parent = player.Transform;

            IEnumerator<float> main()
            {
                while (true)
                {
                    try
                    {
                        if (player.TryGetLookPlayer(15, out Player target, out _))
                        {
                            player.DisableEffect(EffectType.Slowness);
                            player.EnableEffect(EffectType.MovementBoost, 20);

                            target.Heal(0.4f);
                        }
                        else
                        {
                            player.DisableEffect(EffectType.MovementBoost);
                            player.EnableEffect(EffectType.Slowness, 20);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Reason: {e.Message}, StackTrace: {e.StackTrace}");
                    }

                    player.Heal(2f);

                    yield return Timing.WaitForSeconds(0.1f);
                }
            }

            IEnumerator<float> makeSound()
            {
                while (true)
                {
                    var audio = Tools.PlaySound(schematic.transform, "scp-999-moving");

                    yield return Timing.WaitForSeconds((float)audio.Duration.TotalSeconds);
                }
            }

            IEnumerator<float> cute()
            {
                while (true)
                {
                    cuteCooldown = false;

                    while (!cuteCooldown)
                    {
                        player.AddHint("SCP-999 cute", "<size=20>[ALT]키를 눌러 애교 부리기</size>", 1);

                        yield return Timing.WaitForSeconds(1);
                    }

                    yield return Timing.WaitForSeconds(Random.Range(9, 100));
                }
            }

            var main_c = Timing.RunCoroutine(main());
            var makeSound_c = Timing.RunCoroutine(makeSound());
            var cute_c = Timing.RunCoroutine(cute());

            void OnTogglingNoClip(TogglingNoClipEventArgs ev)
            {
                if (ev.Player != player)
                    return;

                if (!cuteCooldown)
                {
                    schematic.AnimationController.Stop();
                    string name = $"Normal{Random.Range(1, 3)}";
                    schematic.AnimationController.Play(name);
                    Tools.PlaySound(schematic.transform, "scp-999", 2);

                    foreach (var p in PlayerManager.List.Where(x => Vector3.Distance(player.Position, x.Position) < 15))
                    {
                        p.AddEffect(EffectType.MovementBoost, 15, 7);
                        p.AddEffect(EffectType.Invigorated, 1, 7);
                        if (Random.Range(1, 101) == 44)
                        {
                            p.Kill("SCP-999의 애교에 심장이 버티지 못했습니다.");
                        }
                    }

                    cuteCooldown = true;
                }
            }

            void OnPickingUpItem(PickingUpItemEventArgs ev)
            {
                if (ev.Player == player)
                {
                    if (ev.Pickup.Type.IsWeapon() || new List<ItemType> {
                        ItemType.GrenadeHE,
                        ItemType.SCP018
                    }.Contains(ev.Pickup.Type))
                    {
                        ev.IsAllowed = false;

                        player.AddHint("SCP-999 무기 금지", "<size=20><color=red>SCP-999</color>은(는) 무기를 쥘 수 없습니다.</size>", 3);
                    }
                    else if (ev.Pickup.Type == ItemType.KeycardO5)
                    {
                        ev.IsAllowed = false;

                        player.AddHint("SCP-999 O5 금지", "<size=20><color=red>SCP-999</color>은(는) O5 카드를 쥘 수 없습니다.</size>", 3);
                    }
                }
            }

            void OnSpawningRagdoll(SpawningRagdollEventArgs ev)
            {
                if (ev.Player == player)
                {
                    ev.IsAllowed = false;
                }
            }

            void OnDying(DyingEventArgs ev)
            {
                if (ev.Player != player)
                    return;

                Vector3 pos = ev.Player.Position;

                Timing.CallDelayed(Timing.WaitForOneFrame, () =>
                {
                    if (ev.Player.IsDead)
                    {
                        if (ev.Player == player)
                        {
                            Timing.KillCoroutines(main_c);
                            Timing.KillCoroutines(makeSound_c);
                            Timing.KillCoroutines(cute_c);

                            schematic.Destroy();

                            SchematicObject dead = ObjectSpawner.SpawnSchematic("SCP_999_Dead", new Vector3(pos.x, pos.y + 0.1f, pos.z), player.Rotation);
                            dead.gameObject.AddComponent<BallComponent>();

                            IEnumerator<float> ball()
                            {
                                while (true)
                                {
                                    foreach (Player player in PlayerManager.List.Where(x => x.IsAlive))
                                    {
                                        GameObject _ball = dead.gameObject;

                                        if (Vector3.Distance(_ball.transform.position, player.Position) < 2)
                                        {
                                            _ball.gameObject.TryGetComponent<Rigidbody>(out Rigidbody rig);
                                            rig.AddForce(player.GameObject.transform.forward + new Vector3(0, 0.001f, 0), ForceMode.Impulse);
                                        }
                                    }

                                    yield return Timing.WaitForOneFrame;
                                }
                            }

                            Timing.RunCoroutine(ball());
                            Tools.PlaySound(dead.transform, "scp-999-dead", 2);

                            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
                            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
                            Exiled.Events.Handlers.Player.SpawningRagdoll -= OnSpawningRagdoll;
                            Exiled.Events.Handlers.Player.Dying -= OnDying;
                        }
                    }
                });
            }

            Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
            Exiled.Events.Handlers.Player.SpawningRagdoll += OnSpawningRagdoll;
            Exiled.Events.Handlers.Player.Dying += OnDying;
            return player;
        }
    }
}
