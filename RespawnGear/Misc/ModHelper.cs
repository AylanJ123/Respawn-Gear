using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace RespawnGear.Misc
{
    public static class ModHelper
    {
        #region Global access
        static Mod? _mod;
        public static Mod Mod
        {
            get => _mod ?? throw new Exception("Mod field is null");
            set => _mod = value;
        }

        static ICoreAPI? _api;
        public static ICoreAPI Api
        {
            get => _api ?? throw new Exception("Api field is null");
            set => _api = value;
        }

        public static string ModId => Mod.Info.ModID;

        public static ILogger Logger => Mod.Logger;
        #endregion

        public static T LoadConfig<T>(string filename) => Api.LoadModConfig<T>(filename);
        public static void StoreConfig<T>(T config, string filename) => Api.StoreModConfig(config, filename);

        public class ServerHelper
        {
            static ICoreServerAPI? _serverAPI;
            public static ICoreServerAPI ServerAPI
            {
                get => _serverAPI ?? throw new Exception("Api field is null");
                set => _serverAPI = value;
            }

            public static void Message(IPlayer Player, string message) => ServerAPI.SendMessage(Player, GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
            public static void BroadcastMessage(string message) => ServerAPI.SendMessageToGroup(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);
        }

    }
}
