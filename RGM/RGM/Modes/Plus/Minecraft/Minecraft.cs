using System;
using System.Collections.Generic;
using System.Reflection;
using Exiled.API.Features;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using Exiled.API.Features.Items;
using RGM.API.Features;
using UnityEngine;
using MEC;
using System.Linq;
using Exiled.API.Extensions;
using AdminToys;

namespace RGM.Modes;

[Mode(ModeCategory.Private, ModeInfo.Lock, ModeType.Minecraft)]
public class Minecraft : Mode
{
    public override string Name => "마인크래프트";
    public override string Description => "A mode that turns the map into a Minecraft world.";
    public override string Detail =>
        """
        [ESC] ->[Server Specific Settings] -> 상호작용, 설치, 파괴 키를 할당하세요.
        블록은 설치할 수 있으며, 도구는 파괴 속도에 영향을 줍니다.
        """;
    public override string Color => "#00FF00";
    public override string Author => "Ragdoll";

    public static Minecraft Instance;

    public const float GridSize = 1f;
    public const float MaxDistance = 10f;
    public const int BuildMask = 1 << 0;
    public readonly ItemType MinecraftItemType = ItemType.Coin; // 블록 설치용 아이템

    // 종류 레지스트리: BlockType -> 설계도
    public readonly Dictionary<BlockType, BlockInfo> Blocks = new();

    // 월드: 칸 좌표 -> 설치된 블록 개체
    public readonly Dictionary<Vector3Int, Block> World = new();
    public readonly Dictionary<ushort, BlockData> BlockSerials = new(); //ItemSerial
    private MinecraftEventHandler _eventHandler;
    public override void OnEnabled()
    {
        Instance = this;
        _eventHandler = new MinecraftEventHandler(this);
        Blocks.Clear();

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var blockAttribute = type.GetCustomAttribute<BlockAttribute>();
            if (blockAttribute == null)
                continue;

            if (!typeof(Block).IsAssignableFrom(type))
                continue;

            if (Blocks.TryGetValue(blockAttribute.blockType, out var existing))
            {
                Log.Error(
                    $"Duplicate BlockType '{blockAttribute.blockType}' on {type.FullName}. Already registered by {existing.Type.FullName}.");
                continue;
            }

            Blocks.Add(blockAttribute.blockType, new BlockInfo
            {
                Type = type,
                Name = blockAttribute.Name,
                SchematicName = blockAttribute.SchematicName,
                BlockType = blockAttribute.blockType,
                DestroyTime = blockAttribute.DestroyTime
            });
        }
        Timing.RunCoroutine(OnModeStarted());
        _eventHandler.RegisterEvents();
    }

    public override void OnDisabled()
    {
        foreach (var block in World.Values)
            block.Schematic?.Destroy();

        World.Clear();
        Blocks.Clear();
        _eventHandler.UnRegisterEvents();
    }

    private IEnumerator<float> OnModeStarted()
    {
        yield return Timing.WaitForSeconds(1f);
        foreach (var player in PlayerManager.List.Where(p => p != null && p.IsAlive))
        {
            Spawned(player);
        }
    }

    private static Vector3Int ToGrid(Vector3 pos) => new(
        Mathf.RoundToInt(pos.x / GridSize),
        Mathf.RoundToInt(pos.y / GridSize),
        Mathf.RoundToInt(pos.z / GridSize));

    private static Vector3 ToWorld(Vector3Int grid) => new(
        grid.x * GridSize,
        grid.y * GridSize,
        grid.z * GridSize);

    public bool TryPlaceBlock(Player player, BlockType type)
    {
        if (!Blocks.TryGetValue(type, out var info))
            return false;

        Transform cam = player.CameraTransform;
        if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, MaxDistance, BuildMask))
            return false;

        Vector3Int grid = ToGrid(hit.point + hit.normal * (GridSize * 0.05f));

        if (World.ContainsKey(grid))
            return false;

        var visual = new BlockVisual(info, BlockForm.Placed, ToWorld(grid));
        if (visual.Schematic == null)
        {
            Log.Error($"Schematic '{info.SchematicName}' not found.");
            return false;
        }

        var block = (Block)Activator.CreateInstance(info.Type);
        block.Info = info;
        block.GridPos = grid;
        block.Schematic = visual.Schematic;

        World[grid] = block;
        block.OnPlaced(player, hit);
        return true;
    }

    public bool TryDestroyBlock(Player player)
    {
        if (!TryGetLookedBlock(player, out Block block))
            return false;

        block.OnBroken(player);
        block.Schematic?.Destroy();
        World.Remove(block.GridPos);
        return true;
    }

    private bool TryGetLookedBlock(Player player, out Block block)
    {
        block = null;

        Transform cam = player.CameraTransform;
        if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, MaxDistance, BuildMask))
            return false;

        Vector3Int grid = ToGrid(hit.point - hit.normal * (GridSize * 0.5f));
        return World.TryGetValue(grid, out block);
    }

    public void AddBlock(BlockType type, ushort count, Player player)
    {
        Item item = player.AddItem(MinecraftItemType);
        ushort ItemSerial = item.Serial;

        BlockSerials.Add(ItemSerial, new BlockData { Type = type, Count = count });
    }

    public void Spawned(Player player)
    {
        BlockType RandomBlockType = Tools.EnumToList<BlockType>().Where(t => t != BlockType.Air).GetRandomValue();
        AddBlock(RandomBlockType, (ushort)32, player);
    }
}