using HarmonyLib;
using Vintagestory.API.Common;
using RespawnGear.EntityBehaviors;
using RespawnGear.ItemClasses;

namespace RespawnGear
{
    public class RespawnGearModSystem : ModSystem
    {
        private static RespawnGearModSystem? Instance;

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
            Harmony harmony = new($"{Mod.Info.Authors[0]}:{Mod.Info.ModID}");
            harmony.PatchAll();
        }

        /// <summary> Logs a message and gives it a format </summary>
        /// <param name="isError"> If true, appends a custom string to help during log diving </param>
        public static void Log(string msg, bool isError = false)
        {
            if (Instance != null)
                Instance.Mod.Logger.Notification((isError ? "[RGCUSTOMERROR]" : "") + msg);
            else
                throw new System.Exception("Attempted to access Singleton too early to log: " + msg);
        }

    }
}
