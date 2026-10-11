using AdminToys;
using Exiled.API.Features;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using System;
using UnityEngine;

namespace RGM.Modes;

/// <summary>설치된 블록 하나(개체). 위치와 상태는 여기에 둔다.</summary>
public abstract class Block
{
    public BlockInfo Info { get; internal set; }
    public Vector3Int GridPos { get; internal set; }
    public SchematicObject Schematic { get; internal set; }

    public virtual void OnPlaced(Player player, RaycastHit hit) { }
    public virtual bool OnInteract(Player player) => false; // true면 상호작용이 처리됨
    public virtual void OnBroken(Player player) { }
}

/// <summary>블록 "종류"의 설계도. 모드 시작 때 한 번 만들어 모두가 공유한다.</summary>
public class BlockInfo
{
    public Type Type { get; set; }
    public string Name { get; set; }
    public string SchematicName { get; set; }
    public BlockType BlockType { get; set; }
    public float DestroyTime { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class BlockAttribute(
    string name,
    string SchematicName,
    BlockType blockType,
    float DestroyTime) : Attribute
{
    public string Name { get; } = name;
    public string SchematicName { get; } = SchematicName;
    public BlockType blockType = blockType;
    public float DestroyTime { get; } = DestroyTime;
}

public class BlockData
{
    public BlockType Type { get; set; }
    public ushort Count { get; set; }
}

public enum BlockType
{
    Air,
    WhiteConcrete
}

public enum BlockForm { Placed, Held, Dropped }
public class BlockVisual
{
    public SchematicObject Schematic { get; }
    public BlockForm Form { get; private set; }

    public BlockVisual(BlockInfo info, BlockForm form, Vector3 position)
    {
        Schematic = ObjectSpawner.SpawnSchematic(info.SchematicName, position);
        if (Schematic != null)
            SetForm(form);
    }

    public void SetForm(BlockForm form)
    {
        Form = form;

        // 충돌은 Placed일 때만
        bool collidable = form == BlockForm.Placed;
        foreach (GameObject go in Schematic.AttachedBlocks)
        {
            if (go.TryGetComponent(out PrimitiveObjectToy toy))
                toy.NetworkPrimitiveFlags = collidable
                    ? toy.NetworkPrimitiveFlags | PrimitiveFlags.Collidable
                    : toy.NetworkPrimitiveFlags & ~PrimitiveFlags.Collidable;
        }

        // Placed는 Animator의 기본 상태(Entry → Placed)라 재생할 필요가 없음.
        // 그래야 AnimationController 캐시에 설치된 블록이 쌓이지 않음.
        if (form == BlockForm.Placed)
            return;

        var anim = AnimationController.Get(Schematic);
        if (anim.Animators.Count > 0)
            anim.Play(form.ToString()); // "Held" / "Dropped"
    }

    public void PlaySwing()
    {
        var anim = AnimationController.Get(Schematic);
        if (anim.Animators.Count > 0)
            anim.Animators[0].Play("Swing", 0, 0f); // 매번 처음부터
    }

    public void AttachTo(Transform parent)
    {
        Schematic.transform.parent = parent;
        Schematic.transform.localPosition = Vector3.zero;
        Schematic.transform.localRotation = Quaternion.identity;
    }

    public void Destroy() => Schematic?.Destroy();
}