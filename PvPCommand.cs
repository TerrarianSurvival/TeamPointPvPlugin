using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Terraria;
using Terraria.Localization;
using TShockAPI;

namespace TeamPointPvP
{
    static class PvPCommand
    {
        internal static void ChangeCommand(CommandArgs args)
        {
            Player player = args.TPlayer;
            PvPConfig config = PvPMain.Config;

            var can_chose_classes = new List<int>();
            int count = config.Classes.Count;

            bool gmInclude = false;
            for (int i = 0; i < count; i++)
            {
                config.Classes[i].Parse();
                if (config.Classes[i].Name == "GM-XXXXXX")
                {
                    gmInclude = true;
                    continue;
                }

                if (config.Classes[i].TeamIDs.Contains(player.team))
                {
                    can_chose_classes.Add(i);
                }
            }

            string classSelectErrorMsg = "Invalid class name! Usage: "
                                            + TShock.Config.CommandSpecifier + "change <class_name>\nClass List: "
                                            + string.Join(", ", can_chose_classes.Select(x => config.Classes[x].Name.ToLower(PvPMain.Culture)));
            
            if (!PvPMain.IsInStage(player.Center.X, player.Center.Y))
            {
                if (args.Parameters.Count != 1 || string.IsNullOrEmpty(args.Parameters[0]))
                {
                    args.Player.SendErrorMessage(classSelectErrorMsg);
                    return;
                }
                int old_class = PvPMain.PlayerClass[args.Player.Index];
                string old_class_name = old_class != -1 ? config.Classes.First(x => x.Id == old_class).Name : "None";

                int id = -1;
                string class_name = args.Parameters[0].ToUpperInvariant();
                PvPClass selectClass = null;

                for (int i = 0; i < can_chose_classes.Count; i++)
                {
                    if (config.Classes[can_chose_classes[i]].Name == class_name)
                    {
                        selectClass = config.Classes[can_chose_classes[i]];
                        id = selectClass.Id;
                        PvPMain.PlayerClass[args.Player.Index] = id;
                        break;
                    }
                }
                if (gmInclude && class_name == string.Format(CultureInfo.InvariantCulture, "GM-{0:D6}", PvPMain.GMCODE))
                {
                    selectClass = config.Classes.First(x => x.Name == "GM-XXXXXX");
                    id = selectClass.Id;
                    class_name = "GM-XXXXXX";
                }

                if (id == -1 || selectClass == null)
                {
                    args.Player.SendErrorMessage(classSelectErrorMsg);
                    return;
                }

                TShock.Log.Write(string.Format(PvPMain.Culture, "CHANGECLASS:{0},{1},{2},{3},{4}", player.name, old_class_name, class_name, player.position.X, player.position.Y), System.Diagnostics.TraceLevel.Info);

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

                if (selectClass.Name == class_name)
                {
                    for (int j = 0; j < selectClass.Items.Count; j++)
                    {
                        selectClass.Items[j].Parse();
                        player.inventory[j].SetDefaults(selectClass.Items[j].ItemID);
                        player.inventory[j].prefix = (byte)selectClass.Items[j].Prefix;
                        player.inventory[j].stack = selectClass.Items[j].Stack;
                    }
                    for (int j = 0; j < selectClass.Ammos.Count; j++)
                    {
                        selectClass.Ammos[j].Parse();
                        player.inventory[max_inventory - j].SetDefaults(selectClass.Ammos[j].ItemID);
                        //player.inventory[max_inventory - j].prefix = (byte)selectClass.ammos[j].prefix;
                        player.inventory[max_inventory - j].stack = selectClass.Ammos[j].Stack;
                    }
                    for (int j = 0; j < selectClass.Armors.Count; j++)
                    {
                        selectClass.Armors[j].Parse();
                        int index = selectClass.Armors[j].SlotID;
                        player.armor[index].SetDefaults(selectClass.Armors[j].ItemID);
                        //player.armor[index].prefix = (byte)selectClass.armors[j].prefix;
                        //player.armor[index].stack = selectClass.armors[j].stack;
                    }
                    for (int j = 0; j < selectClass.VanityArmors.Count; j++)
                    {
                        selectClass.VanityArmors[j].Parse();
                        int index = selectClass.VanityArmors[j].SlotID;
                        player.armor[vanity_start + index].SetDefaults(selectClass.VanityArmors[j].ItemID);
                        //player.armor[vanity_start + index].prefix = (byte)selectClass.vanity_armor[j].prefix;
                        //player.armor[vanity_start + index].stack = selectClass.vanity_armor[j].stack;
                    }
                    for (int j = 0; j < selectClass.ArmorDyes.Count; j++)
                    {
                        selectClass.ArmorDyes[j].Parse();
                        player.dye[j].SetDefaults(selectClass.ArmorDyes[j].ItemID);
                        //player.dye[j].prefix = (byte)selectClass.armor_dyes[j].prefix;
                        //player.dye[j].stack = selectClass.armor_dyes[j].stack;
                    }
                    for (int j = 0; j < selectClass.Accessorys.Count; j++)
                    {
                        selectClass.Accessorys[j].Parse();
                        player.armor[acc_start + j].SetDefaults(selectClass.Accessorys[j].ItemID);
                        player.armor[acc_start + j].prefix = (byte)selectClass.Accessorys[j].Prefix;
                        //player.armor[acc_start + j].stack = selectClass.accessorys[j].stack;
                    }
                    for (int j = 0; j < selectClass.VanityAccessorys.Count; j++)
                    {
                        selectClass.VanityAccessorys[j].Parse();
                        player.armor[vanity_start + acc_start + j].SetDefaults(selectClass.VanityAccessorys[j].ItemID);
                        player.armor[vanity_start + acc_start + j].prefix = (byte)selectClass.VanityAccessorys[j].Prefix;
                        //player.armor[vanity_start + acc_start + j].stack = selectClass.vanity_accessorys[j].stack;
                    }
                    for (int j = 0; j < selectClass.AccessoryDyes.Count; j++)
                    {
                        selectClass.AccessoryDyes[j].Parse();
                        player.dye[acc_start + j].SetDefaults(selectClass.AccessoryDyes[j].ItemID);
                        //player.dye[acc_start + j].prefix = (byte)selectClass.accessory_dyes[j].prefix;
                        //player.dye[acc_start + j].stack = selectClass.accessory_dyes[j].stack;
                    }
                    for (int j = 0; j < selectClass.MiscItems.Count; j++)
                    {
                        selectClass.MiscItems[j].Parse();
                        int index = selectClass.MiscItems[j].SlotID;
                        player.miscEquips[index].SetDefaults(selectClass.MiscItems[j].ItemID);
                        //player.miscEquips[index].prefix = (byte)selectClass.misc_items[j].prefix;
                        //player.miscEquips[index].stack = selectClass.misc_items[j].stack;
                    }
                    for (int j = 0; j < selectClass.MiscDyes.Count; j++)
                    {
                        selectClass.MiscDyes[j].Parse();
                        int index = selectClass.MiscDyes[j].SlotID;
                        player.miscDyes[index].SetDefaults(selectClass.MiscDyes[j].ItemID);
                        //player.miscDyes[index].prefix = (byte)selectClass.misc_dyes[j].prefix;
                        //player.miscDyes[index].stack = selectClass.misc_dyes[j].stack;
                    }
                    player.statLifeMax = selectClass.Hp;
                    player.statManaMax = selectClass.Mp;
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

                PvPMain.SetBuffs(id, playerIndex);

                return;
            }
            else
            {
                args.Player.SendErrorMessage("You can\'t change class in the battlefield.");
            }
        }

        internal static void AllCommand(CommandArgs args)
        {
            TSPlayer tSPlayer = args.Player;
            if (tSPlayer.mute)
            {
                tSPlayer.SendErrorMessage("You are muted!");
                return;
            }
            string msg = string.Format(PvPMain.Culture, "{0}: {1}", args.Player.Name, string.Join(" ", args.Parameters));
            NetMessage.BroadcastChatMessage(NetworkText.FromLiteral(msg), Color.White);
            //TSPlayer.All.SendMessage(msg, 255, 255, 255);
        }

        internal static void GMCodeCommand(CommandArgs args)
        {
            args.Player.SendSuccessMessage(string.Format(CultureInfo.InvariantCulture, "{0:D6}", PvPMain.GMCODE));
        }
    }
}
