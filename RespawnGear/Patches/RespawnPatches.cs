using HarmonyLib;
using RespawnGear.EntityBehaviors;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace RespawnGear.Patches
{
    public class RespawnPatches
    {
        [HarmonyPatch(typeof(ServerMain))]
        [HarmonyPatch(nameof(ServerMain.GetSpawnPosition))]
        class GetSpawnPositionPatch
        {
            static bool Prefix(ServerMain __instance, ref FuzzyEntityPos __result, string playerUID)
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

        //[HarmonyPatch(typeof(ServerSystemEntitySimulation))]
        //[HarmonyPatch(nameof(ServerSystemEntitySimulation.))]
        //class GetSpawnPositionPatch
        //{
        //    static bool Prefix(ServerMain __instance, ref FuzzyEntityPos __result, string playerUID)
        //    {
        //        Entity player = __instance.PlayerByUid(playerUID).Entity;
        //        EntityBehaviorRespawnable respawnable = player.GetBehavior<EntityBehaviorRespawnable>();
        //        if (respawnable.Charges > 0)
        //        {
        //            FuzzyEntityPos fuzzyEntityPos = new FuzzyEntityPos(
        //                respawnable.PosX, respawnable.PosY, respawnable.PosZ
        //            );
        //            __result = fuzzyEntityPos;
        //        }
        //        return true;
        //    }
        //}

        //[HarmonyPatch(typeof(ServerSystemEntitySimulation))]
        //[HarmonyPatch(nameof(ServerSystemEntitySimulation.OnPlayerRespawn))]
        //class GetSpawnPositionPatch
        //{
        //    static bool Prefix(ServerMain __instance, ref FuzzyEntityPos __result, string playerUID)
        //    {
        //        Entity player = __instance.PlayerByUid(playerUID).Entity;
        //        EntityBehaviorRespawnable respawnable = player.GetBehavior<EntityBehaviorRespawnable>();
        //        if (respawnable.Charges > 0)
        //        {
        //            FuzzyEntityPos fuzzyEntityPos = new FuzzyEntityPos(
        //                respawnable.PosX, respawnable.PosY, respawnable.PosZ
        //            );
        //            __result = fuzzyEntityPos;
        //        }
        //        return true;
        //    }
        //}

    }
}
