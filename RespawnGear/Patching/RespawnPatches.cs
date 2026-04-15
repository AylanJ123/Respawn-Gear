using RespawnGear.EntityBehaviors;
using RespawnGear.Misc;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Server;

namespace RespawnGear.Patching
{
    public static class RespawnPatches
    {
        public static class SpawnPositionPatch
        {
            private static Dictionary<string, double> playerDeaths = [];

            public static bool Prefix(ServerMain __instance, ref FuzzyEntityPos __result, string playerUID)
            {
                EntityPlayer playerEntity = __instance.PlayerByUid(playerUID).Entity;
                if (playerEntity.World == null || playerEntity.World.Side != EnumAppSide.Server) return true;
                if (playerEntity.Alive) return true;
                EntityBehaviorRespawnable? respawnable = playerEntity.GetBehavior<EntityBehaviorRespawnable>();
                if (respawnable != null && respawnable.Position != null) // Prev versions compatibility, only value that can be null
                {
                    respawnable.CalculateTimestampAndCharges();
                    if (respawnable.Timestamp == -1) return true;
                    if (respawnable.Charges <= 0)
                    {
                        SendMessage(playerEntity.Player, $"The player {playerEntity.GetName()} has no charges left, into the wilderness you go");
                        return true;
                    }
                    double now = playerEntity.World.Calendar.TotalHours;
                    if (playerDeaths.TryGetValue(playerUID, out double value) && now - value < 1f / 60f) return true;
                    playerDeaths[playerUID] = now;
                    respawnable.Charges -= 1;
                    SendMessage(playerEntity.Player, $"The player {playerEntity.GetName()} has {respawnable.Charges} charges left");
                    FuzzyEntityPos fuzzyEntityPos = new(
                        respawnable.Position.X, respawnable.Position.Y, respawnable.Position.Z
                    ) {
                        Yaw = respawnable.Yaw,
                        Pitch = respawnable.Pitch,
                    };
                    __result = fuzzyEntityPos;
                    return false;
                }
                return true;
            }

            private static void SendMessage(IPlayer player, string msg)
            {
                if (RespawnGearModSystem.Config.PublicHumiliation)
                    ModHelper.ServerHelper.BroadcastMessage(msg);
                else
                    ModHelper.ServerHelper.Message(player, msg);
            }

        }
    }
}
