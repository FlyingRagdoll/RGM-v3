using System;
using System.Collections.Generic;
using System.Linq;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using InventorySystem.Items.Firearms.Attachments;
using MEC;
using UnityEngine;
using Exiled.Events.EventArgs.Scp079;
using Exiled.API.Extensions;
using Exiled.Events.EventArgs.Scp1507;
using static RGM.Variables.Variable;
using Exiled.API.Enums;
using Exiled.Events.EventArgs.Server;
using PlayerRoles;
using RGM.API.Features;
using Random = UnityEngine.Random;

namespace RGM.Modes;

public class MinecraftEventHandler(Minecraft minecraft) 
{
    private readonly Dictionary<Player, BlockVisual> _held = new();

    internal void RegisterEvents()
    {
        Exiled.Events.Handlers.Player.Spawned += OnSpawned;
        Exiled.Events.Handlers.Player.ChangedItem += OnChangedItem;
        Exiled.Events.Handlers.Player.DroppedItem += OnDroppedItem;
        Exiled.Events.Handlers.Player.ItemAdded += OnItemAdded;
        Exiled.Events.Handlers.Player.Left += OnLeft;
        Exiled.Events.Handlers.Player.Died += OnDied;
    }
    internal void UnRegisterEvents()
    {
        Exiled.Events.Handlers.Player.Spawned -= OnSpawned;
        Exiled.Events.Handlers.Player.ChangedItem -= OnChangedItem;
        Exiled.Events.Handlers.Player.DroppedItem -= OnDroppedItem;
        Exiled.Events.Handlers.Player.ItemAdded -= OnItemAdded;
        Exiled.Events.Handlers.Player.Left -= OnLeft;
        Exiled.Events.Handlers.Player.Died -= OnDied;
    }

    private void OnChangedItem(ChangedItemEventArgs ev)
    {
        RemoveHeld(ev.Player); // 무엇을 들든 기존 Held는 먼저 제거

        if (ev.Item == null || !minecraft.BlockSerials.TryGetValue(ev.Item.Serial, out var data))
            return;

        var visual = new BlockVisual(minecraft.Blocks[data.Type], BlockForm.Held, ev.Player.Position);
        visual.AttachTo(ev.Player.Transform);
        _held[ev.Player] = visual;
    }

    private void OnDroppedItem(DroppedItemEventArgs ev)
    {
        if (!minecraft.BlockSerials.TryGetValue(ev.Pickup.Serial, out var data))
            return;

        var visual = new BlockVisual(minecraft.Blocks[data.Type], BlockForm.Dropped, ev.Pickup.Position);
        visual.AttachTo(ev.Pickup.Transform);
    }
    private void OnItemAdded(ItemAddedEventArgs ev)
    {
        // 블록 아이템이 아니면 무시 (모드가 지급할 때는 아직 BlockSerials에 없어서 여기서 걸러짐)
        if (!minecraft.BlockSerials.TryGetValue(ev.Item.Serial, out BlockData picked))
            return;

        BlockData owned = null;
        foreach (var item in ev.Player.Items)
        {
            if (item.Serial == ev.Item.Serial)
                continue; // 방금 들어온 자기 자신은 제외

            if (minecraft.BlockSerials.TryGetValue(item.Serial, out BlockData data) && data.Type == picked.Type)
            {
                owned = data;
                break;
            }
        }

        if (owned == null)
            return; // 같은 종류가 없으면 새 스택으로 유지

        owned.Count += picked.Count;                  // 개수 합치기
        minecraft.BlockSerials.Remove(ev.Item.Serial);
        ev.Player.RemoveItem(ev.Item);                // 방금 주운 동전 삭제
    }

    private void OnLeft(LeftEventArgs ev) => RemoveHeld(ev.Player);
    private void OnDied(DiedEventArgs ev) => RemoveHeld(ev.Player);

    public void SwingHeld(Player player)
    {
        if (_held.TryGetValue(player, out var visual))
            visual.PlaySwing();
    }

    private void RemoveHeld(Player player)
    {
        if (player != null && _held.TryGetValue(player, out var visual))
        {
            visual.Destroy();
            _held.Remove(player);
        }
    }

    private void OnSpawned(SpawnedEventArgs ev)
    {
        minecraft.Spawned(ev.Player);
    }
}