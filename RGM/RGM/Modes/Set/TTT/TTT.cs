using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using MEC;
using UnityEngine;
using RGM.API.Features;
using RGM.Modes.TTTRoles;

using PlayerRoles;
using RGM.API.DataBases;
using Exiled.API.Extensions;
using static RGM.Variables.Variable;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Server;
using InventorySystem.Items.Usables.Scp330;
using Random = UnityEngine.Random;

namespace RGM.Modes
{
    [Mode(ModeCategory.Public, ModeInfo.Set, ModeType.TTT)]
    class TTT : Mode
    {
        public override string Name => "TTT";
        public override string Description => "테러리스트 타운에서 일어난 마피아 게임 (자세한 설명 필독)";
        public override string Detail =>
$"""
{_desc}
""";
        public override string Color => "F78181";

        public static TTT Instance;

        private readonly string _desc =
$"""
Trouble in Terrorist Town의 약자.

<b><size=30>[승리 조건]</size></b>
<color={RoleTypeId.ClassD.GetColor().ToHex()}>무죄인 팀</color>(탐정, 무죄인) - <color=red>배신자</color>들을 처단하세요.
<color=red>배신자 팀</color>(배신자) - <color=red>배신자 팀</color> 구성원을 제외한 나머지를 모두 사살하세요.

<b><size=30>[참고]</size></b>
• 탐정은 <color={RoleTypeId.FacilityGuard.GetColor().ToHex()}>시설 경비</color>의 모습을 하고 있습니다. <color={RoleTypeId.ClassD.GetColor().ToHex()}>무죄인</color>들은 가급적이면 그의 명령을 따라야 합니다.
• 가끔씩 진통제, 고폭 수류탄, 섬광탄이 추가로 지급될 수 있습니다.
• <b><color={RoleTypeId.ClassD.GetColor().ToHex()}>무죄인</color>은 잘못된 유저를 죽이면 심각한 피해를 입습니다!</b>
• <color=#c753d9>소울메이트</color>들은 서로의 위치를 확인할 수 있습니다.
• <color=#000000>O5 평의회</color>는 혼자서 살아남아야 하는 대신, 많은 체력과 아이템들을 가지고 시작합니다.
• <color=#f178fc>광대</color>도 혼자서 살아남아야 하는 대신, <color={RoleTypeId.ClassD.GetColor().ToHex()}>무죄인</color>에게 사망하면 1번 부활합니다.

<b>[Map Credit]</b>
@vasileii, @sleeplessbutter
""";

        public bool _isTTTStarted;
        private Player _detective;
        private Player _o5;
        private Player _jester;
        private List<Player> _traitors = [];
        private List<Player> _soulMates = [];
        private List<Player> _innocentRewardTargets = [];
        private List<Player> _instantKillCooldown = [];
        public Dictionary<Player, TttRole> _playerRoles = new Dictionary<Player, TttRole>();
        public Dictionary<TttRoleType, string> _teamRankColors = new Dictionary<TttRoleType, string>(){
            { TttRoleType.Innocent, "orange" },
            { TttRoleType.Detective, "cyan" },
            { TttRoleType.Soulmate, "pink" },
            { TttRoleType.Traitor, "red" },
            { TttRoleType.Jester, "pink" },
            { TttRoleType.O5, "brown" }
        };

        public Dictionary<TttRoleType, string> _teamColors = new Dictionary<TttRoleType, string>(){
            { TttRoleType.Innocent, RoleTypeId.ClassD.GetColor().ToHex() },
            { TttRoleType.Detective, "#2ECCFA" },
            { TttRoleType.Soulmate, "#c753d9" },
            { TttRoleType.Traitor, "red" },
            { TttRoleType.Jester, "#f178fc" },
            { TttRoleType.O5, "#000000" }
        };

        public readonly List<ItemType> _tttprimary =
        [
            ItemType.GunAK,
            ItemType.GunShotgun,
            ItemType.GunE11SR,
            ItemType.GunLogicer,
            ItemType.GunFRMG0
        ];

        public readonly List<ItemType> _tttsecondary =
        [
            ItemType.GunRevolver,
            ItemType.GunCOM18,
            ItemType.GunCrossvec
        ];

        private CoroutineHandle _onModeStarted;

        public override void OnEnabled()
        {
            Server.FriendlyFire = true;
            Round.IsLocked = true;
            Respawn.PauseWaves();

            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;

            Exiled.Events.Handlers.Player.Shot += OnShot;
            Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;
            Exiled.Events.Handlers.Player.Died += OnDied;

            _onModeStarted = Timing.RunCoroutine(OnModeStarted());
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;

            Exiled.Events.Handlers.Player.Shot -= OnShot;
            Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;
            Exiled.Events.Handlers.Player.Died -= OnDied;

            Timing.KillCoroutines(_onModeStarted);
        }

        private void Spawn(Player player)
        {
            GodModePlayers.Add(player);

            player.Role.Set(RoleTypeId.ClassD);
            player.AddItem(_tttprimary.GetRandomValue());
            player.AddItem(_tttsecondary.GetRandomValue());
            player.Position = Tools.GetObjectList("Spot Random").GetRandomValue().position;
            switch (Convert.ToByte(Random.Range(1, 101)))
            {
                case 1:
                    player.AddCandy(CandyKindID.Pink);
                    break;

                case >= 2 and <= 16:
                    player.AddItem(ItemType.GrenadeHE);
                    break;

                case >= 17 and <= 33:
                    player.AddItem(ItemType.Painkillers);
                    break;
                            
                case >= 34 and <= 50:
                    player.AddItem(ItemType.Medkit);
                    break;
            }
        }

        private List<Player> GetInnocentRewardTargets()
        {
            return PlayerManager.List.Where(x =>
                _innocentRewardTargets.Contains(x) ||
                x.RankName == "무죄인" ||
                x.RankName == "탐정" ||
                x.RankName == "소울메이트"
            ).ToList();
        }

        private IEnumerator<float> OnModeStarted()
        {
            Tools.LoadMap($"{Maps.GetRandomValue()}");

            foreach (var player in PlayerManager.List)
            {
                Spawn(player);
            }

            for (int i = 0; i < 30; i++)
            {
                foreach (var player in PlayerManager.List)
                    player.AddHint("TTT 안내", $"""
                                              <align=left><size=25>
                                              {_desc}
                                              </size></align>

                                              {30 - i}초 후 게임이 시작됩니다.



                                              """, 1.05f);

                yield return Timing.WaitForSeconds(1f);
            }

            Timing.RunCoroutine(Timer());

            foreach (var player in PlayerManager.List.Where(x => x.IsDead))
            {
                Spawn(player);
            }

            GodModePlayers.Clear();
            _innocentRewardTargets.Clear();

            byte traitorCount = 0;
            var playerCount = PlayerManager.List.Count;

            switch (playerCount)
            {
                case >= 2 and <= 5:
                    traitorCount = 1;
                    break;
                case >= 6 and <= 12:
                    traitorCount = 2;
                    break;
                case >= 13 and <= 19:
                    traitorCount = 3;
                    break;
                case >= 20 and <= 26:
                    traitorCount = 4;
                    break;
                case >= 27 and <= 30:
                    traitorCount = 5;
                    break;
                case >= 31 and <= 35:
                    traitorCount = 6;
                    break;
            }

            for (int i = 0; i < traitorCount; i++)
            {
                Player traitor = PlayerManager.List.Where(x => !_traitors.Contains(x)).GetRandomValue();

                _traitors.Add(traitor);
            }

            if (playerCount >= 20)
            {
                _jester = PlayerManager.List.Where(x => !_traitors.Contains(x)).GetRandomValue();
            }

            if (playerCount >= 15)
            {
                for (int i = 0; i < 2; i++)
                {
                    Player soulMate = PlayerManager.List.Where(x => !_traitors.Contains(x) && !_soulMates.Contains(x) && _jester != x).GetRandomValue();

                    _soulMates.Add(soulMate);
                }

                _o5 = PlayerManager.List.Where(x => !_traitors.Contains(x) && !_soulMates.Contains(x) && _jester != x).GetRandomValue();
            }

            _innocentRewardTargets.Add(_detective);

            foreach (var player in PlayerManager.List)
            {
                if (_traitors.Contains(player))
                {
                    player.AddHint("TTT 배신자", $"당신은 <color=red>배신자</color>입니다. <color=red>배신자</color>들을 제외한 나머지를 모두 사살하세요.\n<size=25>{_traitors.Count()}명의 <color=red>배신자</color>가 존재합니다.\n<b>[ALT]ㅣ근접한 플레이어를 즉시 처형할 수 있습니다. (쿨다운 10초)</b></size>", 20);
                }
                else if (_soulMates.Contains(player))
                {
                    _innocentRewardTargets.Add(player);
                    player.AddHint("TTT 소울메이트", $"당신은 <color=#c753d9>소울메이트</color>입니다. 당신의 짝이 어디있는지 실시간으로 확인할 수 있습니다.", 20);
                }
                else if (player == _detective)
                {
                    player.AddHint("TTT 탐정", $"당신은 <color=#2ECCFA>탐정</color>입니다. <color=red>배신자</color>들을 처단하세요.", 20);
                }
                else if (player == _o5)
                {
                    player.AddHint("TTT O5", $"당신은 <color=#000000>O5 평의회</color>입니다. 끝까지 생존하거나, 나머지를 전부 죽이세요!", 20);
                    player.MaxHealth = 240;
                    player.Health = player.MaxHealth;
                    player.AddItem(ItemType.Painkillers);
                }
                else if (player == _jester)
                {
                    player.AddHint("TTT 광대", $"당신은 <color={_teamColors[TttRoleType.Jester]}>광대</color>입니다. 사망하기 전까지 공격할 수 없으며,\n<color={RoleTypeId.ClassD.GetColor().ToHex()}>무죄인</color>에게 사망할 경우 1번 부활하고, 모두가 당신의 정체에 대해 알게 됩니다.", 20);
                }
                else
                {
                    _innocentRewardTargets.Add(player);
                    player.AddHint("TTT 무죄인", $"당신은 <color={_teamColors[TttRoleType.Innocent]}>무죄인</color>입니다. <color={_teamColors[TttRoleType.Detective]}>탐정</color>과 함께 <color={_teamColors[TttRoleType.Traitor]}>배신자</color>들을 처단하세요.", 20);
                }
            }

            _isTTTStarted = true;
        }

        private IEnumerator<float> Timer()
        {
            for (int i = 1; i < 480; i++)
            {
                if (Round.IsEnded)
                    yield break;

                PlayerManager.List.ToList().ForEach(x => x.AddBroadcast(1, $"<size=25>게임 종료까지 {480 - i}초</size>"));

                yield return Timing.WaitForSeconds(1f);
            }

            // 게임 종료 시점: 무죄인이 살아있으면 무죄인 승리
            var innocents = PlayerManager.List.Where(x =>
                !_traitors.Contains(x) &&
                x != _o5 &&
                x != _jester &&
                x.IsAlive
            ).ToList();

            if (innocents.Count > 0)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsAlive && !innocents.Contains(x)))
                {
                    if (GodModePlayers.Contains(player))
                        GodModePlayers.Remove(player);

                    player.Kill("무죄인 팀이 승리하였습니다!");
                }

                foreach (var player in innocents)
                {
                    player.AddBroadcast(20, $"<color=orange>무죄인</color> 팀의 승리입니다!");
                }
                Timing.RunCoroutine(Tools.SetWinner(GetInnocentRewardTargets(), 1));
                yield break;
            }

            // 무죄인 없으면 광대 또는 O5 평의회 중에서 승리자 결정
            if (_jester != null && _jester.IsAlive)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x != _jester))
                {
                    if (GodModePlayers.Contains(player))
                        GodModePlayers.Remove(player);

                    player.Kill("광대가 승리를 탈취해갔습니다!");
                }
                _jester.AddBroadcast(20, $"<color=#f178fc>광대</color>의 승리입니다!");
                Timing.RunCoroutine(Tools.SetWinner(PlayerManager.List.Where(x => x == _jester).ToList(), 13));
            }
            else if (_o5 != null && _o5.IsAlive)
            {
                foreach (var player in PlayerManager.List.Where(x => x.IsAlive && x != _o5))
                {
                    if (GodModePlayers.Contains(player))
                        GodModePlayers.Remove(player);

                    player.Kill("아뿔싸! O5 평의회가 살아있었군요!");
                }
                _o5.AddBroadcast(20, $"<color=#000000>O5 평의회</color>의 승리입니다!");
                Timing.RunCoroutine(Tools.SetWinner(PlayerManager.List.Where(x => x == _o5).ToList(), 8));
            }
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            foreach (var player in PlayerManager.List)
            {
                if (_playerRoles.TryGetValue(player, out var playerRole))
                {
                    player.RankName = playerRole.Name;
                    player.RankColor = $"{_teamRankColors[playerRole.RoleType]}";
                }
            }
        }

        private void OnShot(ShotEventArgs ev)
        {
            ev.Player.AddAmmo(ev.Firearm.AmmoType, 1);
        }

        private void OnTogglingNoClip(TogglingNoClipEventArgs ev)
        {
            ev.Player.Grab();
        }

        private void OnDied(DiedEventArgs ev)
        {
            if (Round.IsEnded || !_isTTTStarted)
                return;

            else if (_soulMates.Contains(ev.Player))
            {
                ev.Player.RankName = "소울메이트";
                ev.Player.RankColor = "pink";

                _soulMates.Remove(ev.Player);
            }
            else if (ev.Player == _o5)
            {
                ev.Player.RankName = "O5 평의회";
                ev.Player.RankColor = "brown";
            }

            if (_traitors.Count(x => x.IsAlive) == 0 && !PlayerManager.List.Any(x => (x == _o5 || x == _jester) && x.IsAlive))
            {
                Round.IsLocked = false;

                if (_detective != null && _detective.IsAlive)
                    _detective.Role.Set(RoleTypeId.ClassD, RoleSpawnFlags.None);

                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(20, $"<color=orange>무죄인</color> 팀의 승리입니다!");
                }
                Timing.RunCoroutine(Tools.SetWinner(GetInnocentRewardTargets(), 1));
            }
            else if (PlayerManager.List.Count(x => x.IsAlive) == 1 && PlayerManager.List.FirstOrDefault(x => x.IsAlive) == _jester)
            {
                Round.IsLocked = false;

                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(20, $"<color=#f178fc>광대</color>의 승리입니다!");
                }

                Timing.RunCoroutine(Tools.SetWinner(PlayerManager.List.Where(x => x == _jester).ToList(), 13));
            }
            else if (PlayerManager.List.Count(x => x.IsAlive) == 1 && PlayerManager.List.FirstOrDefault(x => x.IsAlive) == _o5)
            {
                Round.IsLocked = false;

                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(20, $"<color=#000000>O5 평의회</color>의 승리입니다!");
                }
                Timing.RunCoroutine(Tools.SetWinner(PlayerManager.List.Where(x => x == _o5).ToList(), 8));
            }
            else if (PlayerManager.List.Where(x => !_traitors.Contains(x)).Count(x => x.IsAlive) == 0)
            {
                Round.IsLocked = false;

                foreach (var player in PlayerManager.List)
                {
                    player.AddBroadcast(20, $"<color=red>배신자</color> 팀의 승리입니다!");
                }
                Timing.RunCoroutine(Tools.SetWinner(PlayerManager.List.Where(_traitors.Contains).ToList(), 4));
            }
        }

        public int GetTraitorCount(int count) => count switch
        {
            >= 2 and <= 5 => 1,
            >= 6 and <= 12 => 2,
            >= 13 and <= 19 => 3,
            >= 20 and <= 26 => 4,
            >= 27 and <= 30 => 5,
            _ => 6
        };

        public int GetInnocentRoleCount(int count) => count switch
        {
            >= 2 and <= 5 => 1,
            >= 6 and <= 12 => 3,
            >= 13 and <= 19 => 4,
            >= 20 and <= 26 => 5,
            >= 27 and <= 30 => 7,
            _ => 8
        };

        public int GetNeutralRoleCount(int count) => count switch
        {
            >= 2 and <= 5 => 0,
            >= 6 and <= 19=> 1,
            >= 20 and <= 30 => 2,
            _ => 3
        };
    }
}

//내기 전에 체크해야되는거

