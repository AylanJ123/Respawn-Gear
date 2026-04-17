using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using RespawnGear.EntityBehaviors;
using RespawnGear.ItemClasses;
using RespawnGear.Misc;

namespace RespawnGear
{
    public class RespawnGearModSystem : ModSystem
    {
        private static Harmony? Harmony;

        private static ModConfig? _config;
        public static ModConfig Config => _config ?? throw new Exception("Config field is null");

        public override void Start(ICoreAPI api)
        {
            // Initialize the mod
            base.Start(api);

            ModHelper.Api = api;
            ModHelper.Mod = Mod;
            if (api.Side == EnumAppSide.Server)
                ModHelper.ServerHelper.ServerAPI = (ICoreServerAPI) api;
            _config = ModConfig.Get();

            // Register everything to the API
            api.RegisterItemClass(
                $"{Mod.Info.ModID}:{ItemRespawnGear.ITEM_ID}",
                typeof(ItemRespawnGear)
            );

            // Patch with Harmony
            Harmony = new($"{Mod.Info.Authors[0]}:{Mod.Info.ModID}");
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            CommandsHandler.InitializeCommands(api);

            api.RegisterEntityBehaviorClass(
                $"{Mod.Info.ModID}:{EntityBehaviorRespawnable.BEHAVIOR_ID}",
                typeof(EntityBehaviorRespawnable)
            );

            Harmony?.PatchAll();
        }

        public override void Dispose()
        {
            Harmony?.UnpatchAll(Harmony.Id);
            base.Dispose();
        }
    }
}
