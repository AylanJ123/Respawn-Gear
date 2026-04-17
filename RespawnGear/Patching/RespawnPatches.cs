using HarmonyLib;
using RespawnGear.EntityBehaviors;
using RespawnGear.Misc;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Common;
using Vintagestory.Server;

namespace RespawnGear.Patching
{
    public static class RespawnPatches
    {
        [HarmonyPatch(typeof(ServerMain))]
        [HarmonyPatch(nameof(ServerMain.GetSpawnPosition))]
        public static class SpawnPositionPatch
        {
            public static bool Prefix(ServerMain __instance, ref FuzzyEntityPos __result, string playerUID, bool onlyGlobalDefaultSpawn, bool consumeSpawn)
            {
                bool success = __instance.PlayerDataManager.PlayerDataByUid
                    .TryGetValue(playerUID, out ServerPlayerData? serverPlayerData);
                if (!success || serverPlayerData == null) return true;

                ServerPlayer? serverPlayer = __instance.PlayerByUid(playerUID) as ServerPlayer;
                EntityPlayer? playerEntity = serverPlayer?.Entity;
                if (playerEntity == null || playerEntity.Alive) return true;

                EntityBehaviorRespawnable? respawnable = playerEntity.GetBehavior<EntityBehaviorRespawnable>();
                ServerWorldPlayerData? playerData = serverPlayer?.WorldData as ServerWorldPlayerData;

                PlayerRole playerRole = serverPlayerData.GetPlayerRole(__instance);
                int tempGearUses = playerData?.SpawnPosition?.RemainingUses ?? 0;

                if (playerRole.ForcedSpawn == null && !onlyGlobalDefaultSpawn) // Respect other rules
                {
                    if (consumeSpawn && respawnable != null && respawnable.Position != null) // Last check is a prev versions compatibility, only value that could be null
                    {
                        respawnable.CalculateTimestampAndCharges();
                        if (respawnable.Timestamp == -1) return true; // Player hasn't set up a point
                        if (respawnable.Charges <= 0)
                        {
                            if (tempGearUses > 0)
                                SendMessage(playerEntity.Player, $"The player {playerEntity.GetName()} has no charges left, but was caught by their temporal gear");
                            else
                                SendMessage(playerEntity.Player, $"The player {playerEntity.GetName()} has no charges left, into the wilderness you go");
                            return true;
                        }

                        respawnable.Charges -= 1;
                        SendMessage(playerEntity.Player, $"The player {playerEntity.GetName()} has {respawnable.Charges} charges left");
                        FuzzyEntityPos fuzzyEntityPos = new(
                            respawnable.Position.X, respawnable.Position.Y, respawnable.Position.Z
                        )
                        {
                            Yaw = respawnable.Yaw,
                            Pitch = respawnable.Pitch,
                            UsesLeft = tempGearUses
                        };

                        __result = fuzzyEntityPos;
                        return false;
                    }
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
