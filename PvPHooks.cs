using System;
using Terraria;
using Terraria.DataStructures;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace TeamPointPvP
{
    class PvPHooks
    {
        private static PvPMain instance;
        internal static void Initialize(PvPMain instance)
        {
            ServerApi.Hooks.GameInitialize.Register(instance, OnInitialize);
            ServerApi.Hooks.ServerLeave.Register(instance, OnLeave);
            ServerApi.Hooks.ServerChat.Register(instance, OnChat);
            ServerApi.Hooks.WireTriggerAnnouncementBox.Register(instance, OnTriggerAnnouncementBox);
            ServerApi.Hooks.NetSendData.Register(instance, OnSendData);

            GetDataHandlers.PlayerTeam += OnChangeTeam;
            GetDataHandlers.PlayerDamage += OnPlayerDamage;
            GetDataHandlers.KillMe += OnKillMe;
            GetDataHandlers.PlayerSpawn += OnSpawn;

            GeneralHooks.ReloadEvent += OnReload;

            PvPHooks.instance = instance;
        }

        internal static void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(instance, OnInitialize);
                ServerApi.Hooks.ServerLeave.Deregister(instance, OnLeave);
                ServerApi.Hooks.ServerChat.Deregister(instance, OnChat);
                ServerApi.Hooks.WireTriggerAnnouncementBox.Deregister(instance, OnTriggerAnnouncementBox);
                ServerApi.Hooks.NetSendData.Deregister(instance, OnSendData);

                GetDataHandlers.PlayerTeam -= OnChangeTeam;
                GetDataHandlers.PlayerDamage -= OnPlayerDamage;
                GetDataHandlers.KillMe -= OnKillMe;
                GetDataHandlers.PlayerSpawn -= OnSpawn;

                GeneralHooks.ReloadEvent -= OnReload;
            }
        }

        #region Hooks

        private static void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("tshock.canchat", PvPCommand.AllCommand, "all"));
            Commands.ChatCommands.Add(new Command("tshock.canchat", PvPCommand.ChangeCommand, "change"));
            Commands.ChatCommands.Add(new Command("tshock.godmode", PvPCommand.GMCodeCommand, "gmcode") { AllowServer = true });
        }

        private static void OnSendData(SendDataEventArgs args)
        {
            switch(args.MsgId)
            {
                case PacketTypes.Teleport:
                    {
                        if (args.number != 0)
                        {
                            break;
                        }
                        int playerIndex = (int)args.number2;
                        float x = args.number3;
                        float y = args.number4;
                        string logText = "TELEPORT:" + string.Join(",", Main.player[playerIndex].name, x, y);
                        TShock.Log.Write(logText, System.Diagnostics.TraceLevel.Info);
                        break;
                    }
            }
        }

        private static void OnTriggerAnnouncementBox(TriggerAnnouncementBoxEventArgs args)
        {
            string logText = string.Format(PvPMain.Culture, "ANNOUNCEMENTBOX:{0}",
                string.Join(",", new object[] { args.Who, args.TileX, args.TileY, args.Text }));
            TShock.Log.Write(logText, System.Diagnostics.TraceLevel.Info);
        }

        private static void OnChangeTeam(object sender, GetDataHandlers.PlayerTeamEventArgs args)
        {
            string logText = string.Format(PvPMain.Culture, "CHANGETEAM:{0}",
                string.Join(",", new object[] { args.Player.Name, args.Player.Team, args.Team, args.Player.TPlayer.position.X, args.Player.TPlayer.position.Y }));
            TShock.Log.Write(logText, System.Diagnostics.TraceLevel.Info);
        }

        private static void OnPlayerDamage(object sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            PlayerDeathReason reason = args.PlayerDeathReason;

            Player victim = Main.player[args.ID];
            Player enemyPlayer = args.Player.TPlayer;

            // Format: DeadPlayer, KillerPlayer, Damage, DeadPlayerX, DeadPlayerY, KillerPlayerX, KillerPlayerY, KillerItem, KillerProj, KillerNPC, KillerOther
            string deadPlayerName = victim.name;
            string killerPlayerName;
            string killerPlayerX;
            string killerPlayerY;

            string projName = reason.SourceProjectileIndex >= 0 ? Lang.GetProjectileName(reason.SourceProjectileType).Value : "";
            string itemName = reason.SourceItemType != 0 ? Lang.GetItemName(reason.SourceItemType).Value : "";
            string npcName = reason.SourceNPCIndex >= 0 ? Main.npc[reason.SourceNPCIndex].GetGivenOrTypeNetName().ToString() : "";
            string otherText = "";

            if (enemyPlayer == null)
            {
                killerPlayerName = "";
                killerPlayerX = "";
                killerPlayerY = "";
            }
            else
            {
                killerPlayerName = enemyPlayer.name;
                killerPlayerX = enemyPlayer.position.X.ToString(PvPMain.Culture);
                killerPlayerY = enemyPlayer.position.Y.ToString(PvPMain.Culture);
            }

            switch (reason.SourceOtherIndex)
            {
                case 0:
                    otherText = "FELL";
                    break;
                case 1:
                    otherText = "DROWNED";
                    break;
                case 2:
                    otherText = "LAVA";
                    break;
                case 3:
                    otherText = "DEFAULT";
                    break;
                case 4:
                    otherText = "SLAIN";
                    break;
                case 5:
                    otherText = "PETRIFIED";
                    break;
                case 6:
                    otherText = "STABBED";
                    break;
                case 7:
                    otherText = "SUFFOCATED";
                    break;
                case 8:
                    otherText = "BURNED";
                    break;
                case 9:
                    otherText = "POISONED";
                    break;
                case 10:
                    otherText = "ELECTROCUTED";
                    break;
                case 11:
                    otherText = "TRIED_TO_ESCAPE";
                    break;
                case 12:
                    otherText = "WAS_LICKED";
                    break;
                case 13:
                    otherText = "TELEPORT_1";
                    break;
                case 14:
                    otherText = "TELEPORT_2_MALE";
                    break;
                case 15:
                    otherText = "TELEPORT_2_FEMALE";
                    break;
                case 254:
                    otherText = "NONE";
                    break;
                case 255:
                    otherText = "SLAIN";
                    break;
            }

            int damage = PvPMain.CalcDamage(victim, args.Damage, args.PVP, args.Critical);

            string logText = string.Format(PvPMain.Culture, "DAMAGED:{0}",
                string.Join(",", new object[] {
                    deadPlayerName,
                    killerPlayerName,
                    damage,
                    victim.position.X,
                    victim.position.Y,
                    killerPlayerX,
                    killerPlayerY,
                    itemName,
                    projName,
                    npcName,
                    otherText,
                }));
            TShock.Log.Write(logText, System.Diagnostics.TraceLevel.Info);
        }

        private static void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            // almost same as OnPlayerDamage, diff: logText, DAMAGED -> DEATH
            PlayerDeathReason reason = args.PlayerDeathReason;
            TSPlayer enemyPlayer = reason.SourcePlayerIndex >= 0 && reason.SourcePlayerIndex < 255
                ? TShock.Players[reason.SourcePlayerIndex] : null;

            // Format: DeadPlayer, KillerPlayer, Damage, DeadPlayerX, DeadPlayerY, KillerPlayerX, KillerPlayerY, KillerItem, KillerProj, KillerNPC, KillerOther
            string deadPlayerName = args.Player.Name;
            string killerPlayerName;
            string killerPlayerX;
            string killerPlayerY;

            string projName = reason.SourceProjectileIndex >= 0 ? Lang.GetProjectileName(reason.SourceProjectileType).Value : "";
            string itemName = reason.SourceItemType != 0 ? Lang.GetItemName(reason.SourceItemType).Value : "";
            string npcName = reason.SourceNPCIndex >= 0 ? Main.npc[reason.SourceNPCIndex].GetGivenOrTypeNetName().ToString() : "";
            string otherText = "";

            if (enemyPlayer == null)
            {
                killerPlayerName = "";
                killerPlayerX = "";
                killerPlayerY = "";
            }
            else
            {
                killerPlayerName = enemyPlayer.Name;
                killerPlayerX = enemyPlayer.X.ToString(PvPMain.Culture);
                killerPlayerY = enemyPlayer.Y.ToString(PvPMain.Culture);
            }

            switch (reason.SourceOtherIndex)
            {
                case 0:
                    otherText = "FELL";
                    break;
                case 1:
                    otherText = "DROWNED";
                    break;
                case 2:
                    otherText = "LAVA";
                    break;
                case 3:
                    otherText = "DEFAULT";
                    break;
                case 4:
                    otherText = "SLAIN";
                    break;
                case 5:
                    otherText = "PETRIFIED";
                    break;
                case 6:
                    otherText = "STABBED";
                    break;
                case 7:
                    otherText = "SUFFOCATED";
                    break;
                case 8:
                    otherText = "BURNED";
                    break;
                case 9:
                    otherText = "POISONED";
                    break;
                case 10:
                    otherText = "ELECTROCUTED";
                    break;
                case 11:
                    otherText = "TRIED_TO_ESCAPE";
                    break;
                case 12:
                    otherText = "WAS_LICKED";
                    break;
                case 13:
                    otherText = "TELEPORT_1";
                    break;
                case 14:
                    otherText = "TELEPORT_2_MALE";
                    break;
                case 15:
                    otherText = "TELEPORT_2_FEMALE";
                    break;
                case 254:
                    otherText = "NONE";
                    break;
                case 255:
                    otherText = "SLAIN";
                    break;
            }

            string logText = string.Format(PvPMain.Culture, "DEATH:{0}",
                string.Join(",", new object[] {
                    deadPlayerName,
                    killerPlayerName,
                    args.Damage,
                    args.Player.X,
                    args.Player.Y,
                    killerPlayerX,
                    killerPlayerY,
                    itemName,
                    projName,
                    npcName,
                    otherText,
                }));
            TShock.Log.Write(logText, System.Diagnostics.TraceLevel.Info);
        }

        private static void OnSpawn(object sender, GetDataHandlers.SpawnEventArgs args)
        {
            int playerIndex = args.PlayerId;
            if (PvPMain.PlayerClass[playerIndex] != -1)
            {
                PvPMain.SetBuffs(PvPMain.PlayerClass[playerIndex], playerIndex);

                TShock.Players[playerIndex].Heal(TShock.Players[playerIndex].TPlayer.statLifeMax);

                TShock.Log.Write("SPAWN:" + string.Join(",", new object[] { args.Player.Name, args.SpawnX, args.SpawnY}), System.Diagnostics.TraceLevel.Info);
            }
        }

        private static void OnReload(ReloadEventArgs e)
        {
            try
            {
                PvPMain.Config = PvPConfig.Read(PvPMain.PVP_CONFIG_PATH);
            }
            catch (Exception ex)
            {
                e.Player.SendErrorMessage(
                    "An error occurred while reloading TeamPointPvP configuration. Check server logs for details.");
                TShock.Log.Error(ex.Message);
            }
        }

        private static void OnLeave(LeaveEventArgs args)
        {
            PvPMain.PlayerClass[args.Who] = -1;
        }

        private static void OnChat(ServerChatEventArgs args)
        {
            //dont use until fixed allcommand bug
            if (args.Handled) return;
            if (TShock.Players[args.Who].TPlayer.team != 0
                && !args.Text.StartsWith(TShock.Config.CommandSpecifier, StringComparison.InvariantCultureIgnoreCase)
                && !args.Text.StartsWith(TShock.Config.CommandSilentSpecifier, StringComparison.InvariantCultureIgnoreCase))
            {
                string text = args.Text;
                Commands.HandleCommand(TShock.Players[args.Who], TShock.Config.CommandSpecifier + "p " + text);
                args.Handled = true;
            }
        }

        #endregion
    }
}
