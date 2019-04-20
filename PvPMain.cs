using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace TeamPointPvP
{
    [ApiVersion(2, 1)]
    public class PvPMain : TerrariaPlugin
    {
        public override string Author => "Miyabi";
        public override string Description => "Team Point PvP";
        public override string Name => "TeamPointPvP";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        private int[] PlayerClass = new int[256];

        private PvPConfig config;

        public PvPMain(Main game) : base(game)
        {

            config = PvPConfig.Read(Path.Combine(TShock.SavePath, "PvP_config.json"));
            //Console.WriteLine(JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
            if (config == null)
            {
                Console.WriteLine("Config is Null");
                config = new PvPConfig();
            }

            int length = PlayerClass.Length;
            for (int i = 0; i < length; i++)
            {
                PlayerClass[i] = -1;
            }
        }

        private int minx;
        private int maxx;
        private int miny;
        private int maxy;
        #region Initialize/Dispose
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);

            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
            }
            base.Dispose(disposing);
        }
        #endregion

        //Coundnot find OnWorldLoaded hook
        private bool tileChecked = false;
        private int[,] stageData = { { 3754, 381, 4008, 490 }, { 3661, 539, 4005, 634 }, { 3727, 1036, 4008, 1133 } };
        private bool IsInStage (float playerX, float playerY)
        {
            int stageLen = stageData.GetLength(0);
            playerX = playerX / 16f;
            playerY = playerY / 16f;

            for (int i = 0; i < stageLen; i++)
            {
                if (stageData[i, 0] < playerX && playerX < stageData[i, 2] &&
                    stageData[i, 1] < playerY && playerY < stageData[i, 3])
                {
                    return true;
                }
            }
            return false;
        }
        private void OnGameUpdate (EventArgs args)
        {
            if (!tileChecked && false)
            {
                /* NOREACH */
                minx = Main.maxTilesX;
                maxx = 0;
                miny = Main.maxTilesY;
                maxy = 0;
                for (int i = 0; i < Main.maxTilesX; i++)
                {
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        //90 = 18 x 5(placeStyle), NXOR LogicGate
                        if (Main.tile[i, j] != null && Main.tile[i, j].type == TileID.LogicGate && Main.tile[i, j].frameY == 90 && Main.tile[i, j].active())
                        {
                            if (i > maxx) maxx = i;
                            if (i < minx) minx = i;
                            if (j > maxy) maxy = j;
                            if (j < miny) miny = j;
                        }
                    }
                }
                ServerApi.LogWriter.PluginWriteLine(this, "battlefield: minx:" + minx + " maxx:" + maxx + " miny:" + miny + " maxy:" + maxy, System.Diagnostics.TraceLevel.Info);
                tileChecked = true;
            }
        }

        private void OnLeave(LeaveEventArgs args)
        {
            PlayerClass[args.Who] = -1;
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.PlayerSpawn)
            {
                int playerIndex = args.Msg.whoAmI;
                //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("index = " + playerIndex + " class : " + PlayerClass[playerIndex]), Color.White);
                if (PlayerClass[playerIndex] != -1)
                {
                    SetBuffs(PlayerClass[playerIndex], playerIndex);

                    TShock.Players[playerIndex].Heal(TShock.Players[playerIndex].TPlayer.statLifeMax);
                    /*
                    bool isSSC = Main.ServerSideCharacter;

                    if (!isSSC)
                    {
                        Main.ServerSideCharacter = true;
                        NetMessage.SendData(7, playerIndex, -1, null, 0, 0f, 0f, 0f, 0, 0, 0); // Import from ChangeStat plugin, Is this really need?
                        TShock.Players[playerIndex].IgnoreSSCPackets = true;
                    }

                    TShock.Players[playerIndex].TPlayer.statLife = TShock.Players[playerIndex].TPlayer.statLifeMax;
                    NetMessage.SendData(16, playerIndex, -1, null, playerIndex, 0f, 0f, 0f, 0, 0, 0);

                    if (!isSSC)
                    {
                        Main.ServerSideCharacter = false;
                        NetMessage.SendData(7, playerIndex, -1, null, 0, 0.0f, 0.0f, 0.0f, 0, 0, 0); // Send world info
                        TShock.Players[playerIndex].IgnoreSSCPackets = false;
                    }
                    */
                }
            }
        }

        private bool SetBuffs (int class_id, int player_index)
        {
            if (config.classes.Count <= class_id || class_id < 0) return false;
            int count = config.classes.Count;
            int buff_count = config.classes[class_id].buffs.Count;
            //Console.Write(config.classes[class_id].buffs[0].buff_name);
            for (int i = 0; i < buff_count; i++)
            {
                config.classes[class_id].buffs[i].Parse();
                //Console.Write(config.classes[class_id].buffs[i].id);
                TShock.Players[player_index].SetBuff(config.classes[class_id].buffs[i].id, 216000);
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
            int count = config.classes.Count;
            for (int i = 0; i < count; i++)
            {
                config.classes[i].Parse();
                if (config.classes[i].team_id.Contains(player.team))
                {
                    can_chose_classes.Add(i);
                }
            }
            string classSelectErrorMsg = "Invalid class name! Usage: " + TShock.Config.CommandSpecifier + "change <class_name>\nClass List: " + string.Join(", ", can_chose_classes.Select(x => config.classes[x].name));
            if (!IsInStage(player.Center.X, player.Center.Y)) //TODO: Use config, or Automaticary set from world info
            {
                if (args.Parameters.Count != 1 || args.Parameters[0] == null || args.Parameters[0] == "")
                {
                    args.Player.SendErrorMessage(classSelectErrorMsg);
                    return;
                }
                int id = 0;
                string class_name = args.Parameters[0].ToLower();
                for (int i = 0; i < can_chose_classes.Count; i++)
                {
                    if (config.classes[can_chose_classes[i]].name == class_name)
                    {
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("index = " + args.Player.Index + " class : " + i), Color.White);
                        PlayerClass[args.Player.Index] = config.classes[can_chose_classes[i]].id;
                        id = config.classes[can_chose_classes[i]].id;
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
                    var klass = config.classes[can_chose_classes[i]];
                    if (klass.name == class_name)
                    {
                        for (int j = 0; j < klass.items.Count; j++)
                        {
                            klass.items[j].Parse();
                            player.inventory[j].SetDefaults(klass.items[j].item_id);
                            player.inventory[j].prefix = (byte)klass.items[j].prefix;
                            player.inventory[j].stack = klass.items[j].stack;
                        }
                        for (int j = 0; j < klass.ammos.Count; j++)
                        {
                            klass.ammos[j].Parse();
                            player.inventory[max_inventory - j].SetDefaults(klass.ammos[j].item_id);
                            //player.inventory[max_inventory - j].prefix = (byte)klass.ammos[j].prefix;
                            player.inventory[max_inventory - j].stack = klass.ammos[j].stack;
                        }
                        for (int j = 0; j < klass.armors.Count; j++)
                        {
                            klass.armors[j].Parse();
                            int index = klass.armors[j].slot_id;
                            player.armor[index].SetDefaults(klass.armors[j].item_id);
                            //player.armor[index].prefix = (byte)klass.armors[j].prefix;
                            //player.armor[index].stack = klass.armors[j].stack;
                        }
                        for (int j = 0; j < klass.vanity_armor.Count; j++)
                        {
                            klass.vanity_armor[j].Parse();
                            int index = klass.vanity_armor[j].slot_id;
                            player.armor[vanity_start + index].SetDefaults(klass.vanity_armor[j].item_id);
                            //player.armor[vanity_start + index].prefix = (byte)klass.vanity_armor[j].prefix;
                            //player.armor[vanity_start + index].stack = klass.vanity_armor[j].stack;
                        }
                        for (int j = 0; j < klass.armor_dyes.Count; j++)
                        {
                            klass.armor_dyes[j].Parse();
                            player.dye[j].SetDefaults(klass.armor_dyes[j].item_id);
                            //player.dye[j].prefix = (byte)klass.armor_dyes[j].prefix;
                            //player.dye[j].stack = klass.armor_dyes[j].stack;
                        }
                        for (int j = 0; j < klass.accessorys.Count; j++)
                        {
                            klass.accessorys[j].Parse();
                            player.armor[acc_start + j].SetDefaults(klass.accessorys[j].item_id);
                            player.armor[acc_start + j].prefix = (byte)klass.accessorys[j].prefix;
                            //player.armor[acc_start + j].stack = klass.accessorys[j].stack;
                        }
                        for (int j = 0; j < klass.vanity_accessorys.Count; j++)
                        {
                            klass.vanity_accessorys[j].Parse();
                            player.armor[vanity_start + acc_start + j].SetDefaults(klass.vanity_accessorys[j].item_id);
                            player.armor[vanity_start + acc_start + j].prefix = (byte)klass.vanity_accessorys[j].prefix;
                            //player.armor[vanity_start + acc_start + j].stack = klass.vanity_accessorys[j].stack;
                        }
                        for (int j = 0; j < klass.accessory_dyes.Count; j++)
                        {
                            klass.accessory_dyes[j].Parse();
                            player.dye[acc_start + j].SetDefaults(klass.accessory_dyes[j].item_id);
                            //player.dye[acc_start + j].prefix = (byte)klass.accessory_dyes[j].prefix;
                            //player.dye[acc_start + j].stack = klass.accessory_dyes[j].stack;
                        }
                        for (int j = 0; j < klass.misc_items.Count; j++)
                        {
                            klass.misc_items[j].Parse();
                            int index = klass.misc_items[j].slot_id;
                            player.miscEquips[index].SetDefaults(klass.misc_items[j].item_id);
                            //player.miscEquips[index].prefix = (byte)klass.misc_items[j].prefix;
                            //player.miscEquips[index].stack = klass.misc_items[j].stack;
                        }
                        for (int j = 0; j < klass.misc_dyes.Count; j++)
                        {
                            klass.misc_dyes[j].Parse();
                            int index = klass.misc_dyes[j].slot_id;
                            player.miscDyes[index].SetDefaults(klass.misc_dyes[j].item_id);
                            //player.miscDyes[index].prefix = (byte)klass.misc_dyes[j].prefix;
                            //player.miscDyes[index].stack = klass.misc_dyes[j].stack;
                        }
                        player.statLifeMax = klass.hp;
                        player.statManaMax = klass.mp;
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
            if (TShock.Players[args.Who].TPlayer.team != 0 && !args.Text.StartsWith(TShock.Config.CommandSpecifier) && !args.Text.StartsWith(TShock.Config.CommandSilentSpecifier))
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
            string msg = string.Format("{0}: {1}", args.Player.Name, String.Join(" ", args.Parameters));
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.White);
            //TSPlayer.All.SendMessage(msg, 255, 255, 255);
        }
    }
}
