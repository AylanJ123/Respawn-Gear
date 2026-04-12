using System;

namespace RespawnGear.Misc
{
    public class ModConfig
    {
        const string FILENAME = "respawngear.json";

        public int InitCharges;
        public int MaxCharges;
        public float HoursPerCharge;
        public bool PublicHumiliation;

        public static ModConfig Get()
        {
            ModConfig config;
            try
            {
                config = ModHelper.LoadConfig<ModConfig>(FILENAME);
                config ??= GetDefault();
            }
            catch (Exception e)
            {
                ModHelper.Logger.Error("Configuration file corrupted - loading default settings! Please fix or delete the file...");
                ModHelper.Logger.Error(e);

                return GetDefault();
            }
            ModHelper.StoreConfig(config, FILENAME);

            return config;
        }

        static ModConfig GetDefault() => new()
        {
            InitCharges = 0,
            MaxCharges = 3,
            HoursPerCharge = 48,
            PublicHumiliation = true
        };
    }
}
