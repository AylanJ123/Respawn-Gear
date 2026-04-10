using HarmonyLib;
using RespawnGear.EntityBehaviors;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace RespawnGear.Patches
{
    public static class RespawnPatches
    {
        public static class SpawnPositionPatch
        {
            public static bool Prefix(ServerMain __instance, ref FuzzyEntityPos __result, string playerUID)
            {
                EntityPlayer playerEntity = __instance.PlayerByUid(playerUID).Entity;
                if (playerEntity.Alive) return true;
                EntityBehaviorRespawnable? respawnable = playerEntity.GetBehavior<EntityBehaviorRespawnable>();
                if (respawnable != null)
                {
                    ICoreServerAPI serverAPI = (ICoreServerAPI) playerEntity.Api;
                    respawnable.CalculateTimestampAndCharges();
                    if (respawnable.Charges <= 0)
                    {
                        serverAPI.SendMessageToGroup( // TODO: This message is hardcoded, put into lang
                            0, $"The player {playerEntity.GetName()} has no charges left, into the wilderness you go",
                            EnumChatType.Notification
                        );
                        return true;
                    }
                    respawnable.Charges -= 1;
                    serverAPI.SendMessageToGroup( // TODO: This message is hardcoded, put into lang
                            0, $"The player {playerEntity.GetName()} has {respawnable.Charges} charges left",
                            EnumChatType.Notification
                        );
                    FuzzyEntityPos fuzzyEntityPos = new(
                        respawnable.PosX, respawnable.PosY, respawnable.PosZ
                    ) {
                        Yaw = respawnable.Yaw,
                        Pitch = respawnable.Pitch,
                    };
                    __result = fuzzyEntityPos;
                    return false;
                }
                return true;
            }
        }
    }
}
