using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features.Doors;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;

namespace RGM.Modes.Abilities.Epic;

/*[Ability("점멸", "[Space + ALT]ㅣ근처 문으로 텔레포트합니다. (쿨타임 10초)",
    AbilityCategory.Epic, AbilityType.EPIC_BLINK)]*/
public class Blink : Ability
{
    int TeleportCooldown = 0;

    public override void OnEnabled() 
        => Exiled.Events.Handlers.Player.TogglingNoClip += OnTogglingNoClip;

    public override void OnDisabled()
        => Exiled.Events.Handlers.Player.TogglingNoClip -= OnTogglingNoClip;

    private void OnTogglingNoClip(TogglingNoClipEventArgs ev)
    {
        if (ev.Player != Owner || TeleportCooldown > 0 || !ev.Player.IsJumping || ev.Player.CurrentRoom.Type == RoomType.Pocket)
            return;

        TeleportCooldown = 10;

        Door nearestDoor = null;
        float radius = 99999;

        foreach (var door in Door.List.Where(x => !x.IsElevator && x.Zone != ZoneType.LightContainment && !x.Type.ToString().Contains("Scp079")))
        {
            float Distance = Vector3.Distance(door.Position, ev.Player.Position);

            if (!(Distance < radius)) continue;
            nearestDoor = door;
            radius = Distance;
        }

        if (nearestDoor == null) return;
        Vector3 pos = nearestDoor.Position;

        ev.Player.Position = new Vector3(pos.x, pos.y + 2, pos.z);

        Timing.CallDelayed(10, () => { TeleportCooldown = 0; });
    }
}
