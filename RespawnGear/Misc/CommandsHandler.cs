using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using RespawnGear.EntityBehaviors;

namespace RespawnGear.Misc
{
    public static class CommandsHandler
    {

        public static void InitializeCommands(ICoreAPI api)
        {
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("respawn").WithAlias("rspw")

                .BeginSubCommand("setcharges").WithAlias("sc")
                    .RequiresPrivilege(Privilege.give)
                    .WithDescription("Sets the player's Respawn Gear charges")
                    .WithArgs(parsers.OnlinePlayer("player"), parsers.Int("charges"))
                    .HandleWith(SetChargesCommHandler)
                .EndSubCommand()

                .BeginSubCommand("cleardata").WithAlias("cd")
                    .RequiresPrivilege(Privilege.ban)
                    .WithDescription("Removes player respawn data (as if they would have never used the Respawn Gear)")
                    .WithArgs(parsers.OnlinePlayer("player"))
                    .HandleWith(ClearDataCommHandler)
                .EndSubCommand()

                .BeginSubCommand("getdata").WithAlias("gd")
                    .RequiresPrivilege(Privilege.ban)
                    .WithDescription("Retrieves another player's data")
                    .WithArgs(parsers.OnlinePlayer("player"))
                    .HandleWith(GetDataCommHandler)
                .EndSubCommand()

                .BeginSubCommand("worldconfig").WithAlias("wc")
                    .RequiresPrivilege(Privilege.selfkill)
                    .WithDescription("Retrieves the server configs for all players")
                    .HandleWith(GetConfigCommHandler)
                .EndSubCommand()

                .RequiresPrivilege(Privilege.selfkill)
                .WithDescription("spawns particles around the player")
                .RequiresPlayer()
                .HandleWith(GetInfoCommHandler);
        }

        #region Handlers
        private static TextCommandResult SetChargesCommHandler(TextCommandCallingArgs args)
        {
            IServerPlayer player = (IServerPlayer) args[0];
            EntityBehaviorRespawnable? behavior = GetRespawnableBehavior(player);
            if (behavior == null) return TextCommandResult.Error("This entity cannot be respawned, is this really a player? If so, contact the developer.");
            int charges = (int) args[1];
            int max = RespawnGearModSystem.Config.MaxCharges;
            charges = Math.Clamp(charges, 0, max);
            behavior.Charges = charges;
            return TextCommandResult.Success($"{player.PlayerName}'s charges have been set to {(charges == max ? $"max ({charges})" : charges)}");
        }

        private static TextCommandResult ClearDataCommHandler(TextCommandCallingArgs args)
        {
            IServerPlayer player = (IServerPlayer) args[0];
            EntityBehaviorRespawnable? behavior = GetRespawnableBehavior(player);
            if (behavior == null) return TextCommandResult.Error("This entity cannot be respawned, is this really a player? If so, contact the developer.");
            behavior.DefaultValues();
            return TextCommandResult.Success($"{player.PlayerName}'s data has been wiped");
        }

        private static TextCommandResult GetDataCommHandler(TextCommandCallingArgs args)
        {
            IServerPlayer player = (IServerPlayer)args[0];
            EntityBehaviorRespawnable? behavior = GetRespawnableBehavior(player);
            if (behavior == null) return TextCommandResult.Error("This entity cannot be respawned, is this really a player? If so, contact the developer.");
            return TextCommandResult.Success($"{player.PlayerName}'s data:\n{FormatPlayerData(behavior)}");
        }

        private static TextCommandResult GetConfigCommHandler(TextCommandCallingArgs args)
        {
            ModConfig config = RespawnGearModSystem.Config;
            return TextCommandResult.Success($"Max charges: {config.MaxCharges}\nHours per charge: {config.HoursPerCharge}");
        }

        private static TextCommandResult GetInfoCommHandler(TextCommandCallingArgs args)
        {
            IPlayer player = args.Caller.Player;
            EntityBehaviorRespawnable? behavior = GetRespawnableBehavior(player);
            if (behavior == null) return TextCommandResult.Error("Somehow, the game thinks you cannot be respawned. Contact the developer.");
            return TextCommandResult.Success(FormatPlayerData(behavior));
        }
        #endregion

        #region Helpers
        private static EntityBehaviorRespawnable? GetRespawnableBehavior(IPlayer player)
        {
            EntityBehaviorRespawnable? behaviorRespawnable = player.Entity.GetBehavior<EntityBehaviorRespawnable>();
            if (player == null)
            {
                ModHelper.Logger.Error("Couldn't find respawnable behavior on " + player?.PlayerName);
                return null;
            }
            else return behaviorRespawnable;
        }

        private static string FormatPlayerData(EntityBehaviorRespawnable behavior)
        {
            behavior.CalculateTimestampAndCharges();
            double currentTime = ModHelper.ServerHelper.ServerAPI.World.Calendar.TotalHours;
            float hoursPerChage = RespawnGearModSystem.Config.HoursPerCharge;
            int mapSizeX = ModHelper.Api.World.Config.GetAsInt("worldWidth");
            int mapSizeZ = ModHelper.Api.World.Config.GetAsInt("worldLength");
            if (behavior.Timestamp == -1) return "Info unavailable, player hasn't set up a respawn location";
            return $"Respawn location: {behavior.Position?.X - mapSizeX / 2}, {behavior.Position?.Y}, {behavior.Position?.Z - mapSizeZ / 2}\nCharge percentage: {(currentTime - behavior.Timestamp) / hoursPerChage * 100:0.##}%\nCharges: {behavior.Charges}";
        }
        #endregion

    }
}
