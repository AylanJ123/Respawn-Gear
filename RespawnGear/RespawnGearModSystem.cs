using HarmonyLib;
using Vintagestory.API.Common;
using RespawnGear.EntityBehaviors;
using RespawnGear.ItemClasses;
using Vintagestory.API.Server;
using Vintagestory.Server;
using System.Reflection;
using RespawnGear.Patching;

namespace RespawnGear
{
    public class RespawnGearModSystem : ModSystem
    {
        private static RespawnGearModSystem? Instance;
        private static Harmony? Harmony;

        public override void Start(ICoreAPI api)
        {
            // Initialize the mod
            Instance = this;
            base.Start(api);
            
            // Register everything to the API
            api.RegisterItemClass(
                $"{Mod.Info.ModID}:{ItemRespawnGear.ITEM_ID}",
                typeof(ItemRespawnGear)
            );
            api.RegisterEntityBehaviorClass(
                $"{Mod.Info.ModID}:{EntityBehaviorRespawnable.BEHAVIOR_ID}",
                typeof(EntityBehaviorRespawnable)
            );

            // Patch with Harmony
            Harmony = new($"{Mod.Info.Authors[0]}:{Mod.Info.ModID}");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            MethodInfo originalMethod = AccessTools.Method(typeof(ServerMain), nameof(ServerMain.GetSpawnPosition));
            MethodInfo prefixMethod = SymbolExtensions.GetMethodInfo(() => RespawnPatches.SpawnPositionPatch.Prefix);
            Harmony?.Patch(originalMethod, new HarmonyMethod(prefixMethod));
        }

        public override void Dispose()
        {
            Harmony?.UnpatchAll(Harmony.Id);
            base.Dispose();
        }

        /// <summary> Logs a message </summary>
        public static void Log(string msg) => Instance?.Mod.Logger.Notification(msg);

        public static void LogError(string msg) => Instance?.Mod.Logger.Error(msg);
    }
}
