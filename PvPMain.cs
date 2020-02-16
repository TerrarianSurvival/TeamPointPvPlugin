using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace TeamPointPvP
{
    [ApiVersion(2, 1)]
    public class PvPMain : TerrariaPlugin
    {
        public override string Author => "Miyabi";
        public override string Description => "Team Point PvP";
        public override string Name => "TeamPointPvP";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private readonly int[] PlayerClass = new int[256];

        private PvPConfig config;
        private readonly string PVP_CONFIG_PATH;

        internal static readonly CultureInfo Culture = new CultureInfo("en-US");

        public PvPMain(Main game) : base(game)
        {
            PVP_CONFIG_PATH = Path.Combine(TShock.SavePath, "PvP_config.json");
            config = PvPConfig.Read(PVP_CONFIG_PATH);

            if (config == null)
            {
                Console.WriteLine("Config is NULL.");
                config = new PvPConfig();
            }

            int length = PlayerClass.Length;
            for (int i = 0; i < length; i++)
            {
                PlayerClass[i] = -1;
            }
        }

        #region Initialize/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            
            GetDataHandlers.KillMe += OnKillMe;
            GetDataHandlers.PlayerSpawn += OnSpawn;
            
            GeneralHooks.ReloadEvent += OnReload;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);

                GetDataHandlers.KillMe -= OnKillMe;
                GetDataHandlers.PlayerSpawn -= OnSpawn;

                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }
        #endregion

        //Couldn't find OnWorldLoaded hook
        private bool mapChecked = false;
        private List<PvPMap.Area> currentBlacklist = new List<PvPMap.Area>();
        private List<PvPMap.Area> currentWhitelist = new List<PvPMap.Area>();
        private bool IsInStage (float playerX, float playerY)
        {
            if (!mapChecked)
            {
                foreach (var map in config.Maps)
                {
                    if (Main.worldName.Contains(map.Name))
                    {
                        currentBlacklist = map.BlackList;
                        currentWhitelist = map.WhiteList;
                        break;
                    }
                }
                mapChecked = true;
            }

            playerX /= 16f;
            playerY /= 16f;

            foreach (var area in currentWhitelist)
            {
                if (area.ContainsPoint(playerX, playerY))
                {
                    return false;
                }
            }

            foreach (var area in currentBlacklist)
            {
                if (area.ContainsPoint(playerX, playerY))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs args)
        {
            PlayerDeathReason reason = args.PlayerDeathReason;
            TSPlayer enemyPlayer = reason.SourcePlayerIndex >= 0 && reason.SourcePlayerIndex < 255
                ? TShock.Players[reason.SourcePlayerIndex] : null;

            // Format: DeadPlayer, KillerPlayer, Damage, DeadTeam, KillerTeam, KillerProj, KillerItem, KillerNPC, KillerOther, DeathText
            string deadPlayerName = args.Player.Name;
            string killerPlayerName;
            string killerPlayerTeam;
            string projName = reason.SourceProjectileIndex >= 0 ? Lang.GetProjectileName(reason.SourceProjectileType).Value : "";
            string itemName = reason.SourceItemType != 0 ? Lang.GetItemName(reason.SourceItemType).Value : "";
            string npcName = reason.SourceNPCIndex >= 0 ? Main.npc[reason.SourceNPCIndex].GetGivenOrTypeNetName().ToString() : "";
            string otherText = "";

            if (enemyPlayer == null)
            {
                killerPlayerName = "";
                killerPlayerTeam = "";
            }
            else
            {
                killerPlayerName = enemyPlayer.Name;
                killerPlayerTeam = enemyPlayer.Team.ToString(Culture);
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

            string logText = string.Format(Culture, "\"{0}\",\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\"",
                deadPlayerName,
                killerPlayerName,
                args.Damage,
                args.Player.Team,
                killerPlayerTeam,
                projName,
                itemName,
                npcName,
                otherText,
                reason.GetDeathText(deadPlayerName).ToString());
            TShock.Log.Write(logText, System.Diagnostics.TraceLevel.Info);
        }

        private void OnSpawn(object sender, GetDataHandlers.SpawnEventArgs e)
        {
            int playerIndex = e.Player.Index;
            if (PlayerClass[playerIndex] != -1)
            {
                SetBuffs(PlayerClass[playerIndex], playerIndex);

                TShock.Players[playerIndex].Heal(TShock.Players[playerIndex].TPlayer.statLifeMax);
            }
        }

        private void OnReload(ReloadEventArgs e)
        {
            try
            {
                config = PvPConfig.Read(PVP_CONFIG_PATH);
            }
            catch (Exception ex)
            {
                e.Player.SendErrorMessage(
                    "An error occurred while reloading TeamPointPvP configuration. Check server logs for details.");
                TShock.Log.Error(ex.Message);
            }
        }

        private void OnLeave(LeaveEventArgs args)
        {
            PlayerClass[args.Who] = -1;
        }

        private void OnGetData(GetDataEventArgs args)
        {
            switch (args.MsgID)
            {
                case PacketTypes.PlayerSpawn:
                case PacketTypes.PlayerSpawnSelf:
                    int playerIndex = args.Msg.whoAmI;
                    if (PlayerClass[playerIndex] != -1)
                    {
                        SetBuffs(PlayerClass[playerIndex], playerIndex);

                        TShock.Players[playerIndex].Heal(TShock.Players[playerIndex].TPlayer.statLifeMax);
                    }
                    break;
            }
        }

        private bool SetBuffs (int class_id, int player_index)
        {
            if (config.Classes.Count <= class_id || class_id < 0)
            {
                return false;
            }

            int buff_count = config.Classes[class_id].Buffs.Count;
            for (int i = 0; i < buff_count; i++)
            {
                config.Classes[class_id].Buffs[i].Parse();
                TShock.Players[player_index].SetBuff(config.Classes[class_id].Buffs[i].id, 216000);
            }
            return true;
        }

        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("tshock.canchat", AllCommand, "all"));
            Commands.ChatCommands.Add(new Command("tshock.canchat", ChangeCommand, "change"));
        }

        private void ChangeCommand(CommandArgs args)
        {
            Player player = args.TPlayer;
            var can_chose_classes = new List<int>();
            int count = config.Classes.Count;
            for (int i = 0; i < count; i++)
            {
                config.Classes[i].Parse();
                if (config.Classes[i].TeamID.Contains(player.team))
                {
                    can_chose_classes.Add(i);
                }
            }
            string classSelectErrorMsg = "Invalid class name! Usage: " + TShock.Config.CommandSpecifier + "change <class_name>\nClass List: " + string.Join(", ", can_chose_classes.Select(x => config.Classes[x].Name));
            if (!IsInStage(player.Center.X, player.Center.Y))
            {
                if (args.Parameters.Count != 1 || string.IsNullOrEmpty(args.Parameters[0]))
                {
                    args.Player.SendErrorMessage(classSelectErrorMsg);
                    return;
                }
                int id = 0;
                string class_name = args.Parameters[0].ToUpperInvariant();
                for (int i = 0; i < can_chose_classes.Count; i++)
                {
                    if (config.Classes[can_chose_classes[i]].Name == class_name)
                    {
                        PlayerClass[args.Player.Index] = config.Classes[can_chose_classes[i]].Id;
                        id = config.Classes[can_chose_classes[i]].Id;
                        break;
                    }
                }
                
                #region ResetAndSetInventory
                // Clear inventory
                for (int invIndex = 0; invIndex < NetItem.InventorySlots; ++invIndex)
                {
                    player.inventory[invIndex] = new Item();
                }
                for (int invIndex = 0; invIndex < NetItem.ArmorSlots; ++invIndex)
                {
                    player.armor[invIndex] = new Item();
                }
                for (int invIndex = 0; invIndex < NetItem.DyeSlots; ++invIndex)
                {
                    player.dye[invIndex] = new Item();
                }
                for (int invIndex = 0; invIndex < NetItem.MiscEquipSlots; ++invIndex)
                {
                    player.miscEquips[invIndex] = new Item();
                }
                for (int invIndex = 0; invIndex < NetItem.MiscDyeSlots; ++invIndex)
                {
                    player.miscDyes[invIndex] = new Item();
                }

                // Clear buffs
                int buffLength = player.buffType.Length;
                for (int buffIndex = 0; buffIndex < buffLength; buffIndex++)
                {
                    player.DelBuff(buffIndex);
                }

                // Clear own projectiles
                int projLength = Main.projectile.Length;
                for (int projIndex = 0; projIndex < projLength; projIndex++)
                {
                    if (Main.projectile[projIndex].owner == player.whoAmI)
                    {
                        Main.projectile[projIndex].timeLeft = 2;
                        Main.projectile[projIndex].netUpdate = true;
                    }
                }

                int num_classes = can_chose_classes.Count;
                const int max_inventory = 49;
                const int vanity_start = 10;
                const int acc_start = 3;
                for (int i = 0; i < num_classes; i++)
                {
                    var klass = config.Classes[can_chose_classes[i]];
                    if (klass.Name == class_name)
                    {
                        for (int j = 0; j < klass.Items.Count; j++)
                        {
                            klass.Items[j].Parse();
                            player.inventory[j].SetDefaults(klass.Items[j].ItemID);
                            player.inventory[j].prefix = (byte)klass.Items[j].Prefix;
                            player.inventory[j].stack = klass.Items[j].Stack;
                        }
                        for (int j = 0; j < klass.Ammos.Count; j++)
                        {
                            klass.Ammos[j].Parse();
                            player.inventory[max_inventory - j].SetDefaults(klass.Ammos[j].ItemID);
                            //player.inventory[max_inventory - j].prefix = (byte)klass.ammos[j].prefix;
                            player.inventory[max_inventory - j].stack = klass.Ammos[j].Stack;
                        }
                        for (int j = 0; j < klass.Armors.Count; j++)
                        {
                            klass.Armors[j].Parse();
                            int index = klass.Armors[j].SlotID;
                            player.armor[index].SetDefaults(klass.Armors[j].ItemID);
                            //player.armor[index].prefix = (byte)klass.armors[j].prefix;
                            //player.armor[index].stack = klass.armors[j].stack;
                        }
                        for (int j = 0; j < klass.VanityArmors.Count; j++)
                        {
                            klass.VanityArmors[j].Parse();
                            int index = klass.VanityArmors[j].SlotID;
                            player.armor[vanity_start + index].SetDefaults(klass.VanityArmors[j].ItemID);
                            //player.armor[vanity_start + index].prefix = (byte)klass.vanity_armor[j].prefix;
                            //player.armor[vanity_start + index].stack = klass.vanity_armor[j].stack;
                        }
                        for (int j = 0; j < klass.ArmorDyes.Count; j++)
                        {
                            klass.ArmorDyes[j].Parse();
                            player.dye[j].SetDefaults(klass.ArmorDyes[j].ItemID);
                            //player.dye[j].prefix = (byte)klass.armor_dyes[j].prefix;
                            //player.dye[j].stack = klass.armor_dyes[j].stack;
                        }
                        for (int j = 0; j < klass.Accessorys.Count; j++)
                        {
                            klass.Accessorys[j].Parse();
                            player.armor[acc_start + j].SetDefaults(klass.Accessorys[j].ItemID);
                            player.armor[acc_start + j].prefix = (byte)klass.Accessorys[j].Prefix;
                            //player.armor[acc_start + j].stack = klass.accessorys[j].stack;
                        }
                        for (int j = 0; j < klass.VanityAccessorys.Count; j++)
                        {
                            klass.VanityAccessorys[j].Parse();
                            player.armor[vanity_start + acc_start + j].SetDefaults(klass.VanityAccessorys[j].ItemID);
                            player.armor[vanity_start + acc_start + j].prefix = (byte)klass.VanityAccessorys[j].Prefix;
                            //player.armor[vanity_start + acc_start + j].stack = klass.vanity_accessorys[j].stack;
                        }
                        for (int j = 0; j < klass.AccessoryDyes.Count; j++)
                        {
                            klass.AccessoryDyes[j].Parse();
                            player.dye[acc_start + j].SetDefaults(klass.AccessoryDyes[j].ItemID);
                            //player.dye[acc_start + j].prefix = (byte)klass.accessory_dyes[j].prefix;
                            //player.dye[acc_start + j].stack = klass.accessory_dyes[j].stack;
                        }
                        for (int j = 0; j < klass.MiscItems.Count; j++)
                        {
                            klass.MiscItems[j].Parse();
                            int index = klass.MiscItems[j].SlotID;
                            player.miscEquips[index].SetDefaults(klass.MiscItems[j].ItemID);
                            //player.miscEquips[index].prefix = (byte)klass.misc_items[j].prefix;
                            //player.miscEquips[index].stack = klass.misc_items[j].stack;
                        }
                        for (int j = 0; j < klass.MiscDyes.Count; j++)
                        {
                            klass.MiscDyes[j].Parse();
                            int index = klass.MiscDyes[j].SlotID;
                            player.miscDyes[index].SetDefaults(klass.MiscDyes[j].ItemID);
                            //player.miscDyes[index].prefix = (byte)klass.misc_dyes[j].prefix;
                            //player.miscDyes[index].stack = klass.misc_dyes[j].stack;
                        }
                        player.statLifeMax = klass.Hp;
                        player.statManaMax = klass.Mp;
                        break;
                    }
                }
               
                #endregion

                if (player.statLife > player.statLifeMax)
                {
                    player.statLife = player.statLifeMax;
                }

                #region SendCharactor

                int playerIndex = args.Player.Index;
                bool isSSC = Main.ServerSideCharacter;

                if (!isSSC)
                {
                    Main.ServerSideCharacter = true;
                    NetMessage.SendData(7, playerIndex, -1, null, 0, 0f, 0f, 0f, 0, 0, 0); // Import from ChangeStat plugin, Is this really need?
                    args.Player.IgnoreSSCPackets = true;
                }

                NetMessage.SendData(16, playerIndex, -1, null, playerIndex, 0f, 0f, 0f, 0, 0, 0); //Send life info
                NetMessage.SendData(42, playerIndex, -1, null, playerIndex, 0f, 0f, 0f, 0, 0, 0); //Send mana info
                player.extraAccessory = true;
                NetMessage.SendData(4, playerIndex, -1, null, playerIndex, 0f, 0f, 0f, 0, 0, 0); //Send charactor info (include extra accessory slot)

                int masterInvIndex = 0;
                for (int invIndex = 0; invIndex < NetItem.InventorySlots; ++invIndex)
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, player.inventory[invIndex].prefix, 0, 0, 0, 0);
                    ++masterInvIndex;
                }
                for (int invIndex = 0; invIndex < NetItem.ArmorSlots; ++invIndex) //Include accessory
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, player.armor[invIndex].prefix, 0, 0, 0, 0);
                    ++masterInvIndex;
                }
                for (int invIndex = 0; invIndex < NetItem.DyeSlots; ++invIndex)
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, player.dye[invIndex].prefix, 0, 0, 0, 0);
                    ++masterInvIndex;
                }
                for (int invIndex = 0; invIndex < NetItem.MiscEquipSlots; ++invIndex) // Hooks, Light Pet, etc...
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, player.miscEquips[invIndex].prefix, 0, 0, 0, 0);
                    ++masterInvIndex;
                }
                for (int invIndex = 0; invIndex < NetItem.MiscDyeSlots; ++invIndex)
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, player.miscDyes[invIndex].prefix, 0, 0, 0, 0);
                    ++masterInvIndex;
                }

                NetMessage.SendData(5, playerIndex, -1, new NetworkText((Main.player[playerIndex]).trashItem.Name, NetworkText.Mode.Formattable), playerIndex, 179f, ((Main.player[playerIndex]).trashItem).prefix, 0.0f, 0, 0, 0);

                NetMessage.SendData(50, -1, -1, null, playerIndex, 0f, 0f, 0f, 0, 0, 0); // Send buff info

                if (!isSSC)
                {
                    Main.ServerSideCharacter = false;
                    NetMessage.SendData(7, playerIndex, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0); // Send world info
                    args.Player.IgnoreSSCPackets = false;
                }

                #endregion

                SetBuffs(id, playerIndex);

                return;
            }
            else
            {
                args.Player.SendErrorMessage("You can\'t change class in the battlefield.");
            }
        }

        private void OnChat(ServerChatEventArgs args)
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

        private void AllCommand(CommandArgs args)
        {
            //it doesnt work
            TSPlayer tSPlayer = args.Player;
            if (tSPlayer.mute)
            {
                tSPlayer.SendErrorMessage("You are muted!");
                return;
            }
            string msg = string.Format(Culture, "{0}: {1}", args.Player.Name, string.Join(" ", args.Parameters));
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.White);
            //TSPlayer.All.SendMessage(msg, 255, 255, 255);
        }
    }
}
