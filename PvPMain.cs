using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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

        public string[] ClassName = { "standard", "killer", "gale", "hoplite", "creeper", "ninja", "desert", "wizard", "frost", "summon" };

        public int[] PlayerClass = new int[256];

        public PvPMain(Main game) : base(game)
        {
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

        //Coundnt find OnWorldLoaded hook
        private bool tileChecked = false;
        private void OnGameUpdate (EventArgs args)
        {
            if (!tileChecked)
            {
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
                    SetBuffs(ClassName[PlayerClass[playerIndex]], playerIndex);
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
                }
            }
        }

        private bool SetBuffs (string className, int playerIndex)
        {
            switch (className)
            {
                case "standard":
                    {
                        return true;
                    }
                case "killer":
                    {
                        return true;
                    }
                case "gale":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.WellFed, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.Tipsy, 216000);
                        return true;
                    }
                case "hoplite":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.Endurance, 216000);
                        return true;
                    }
                case "creeper":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.Regeneration, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.Slow, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.Chilled, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.BrokenArmor, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.Ichor, 216000);
                        return true;
                    }
                case "ninja":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.Endurance, 216000);
                        return true;
                    }
                case "desert":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.MagicPower, 216000);
                        return true;
                    }
                case "wizard":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.MagicPower, 216000);
                        return true;
                    }
                case "frost":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.MagicPower, 216000);
                        return true;
                    }
                case "summon":
                    {
                        TShock.Players[playerIndex].SetBuff(BuffID.WellFed, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.Wrath, 216000);
                        TShock.Players[playerIndex].SetBuff(BuffID.Ironskin, 216000);
                        return true;
                    }
                default:
                    return false;
            }
        }

        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("tshock.canchat", AllCommand, "all"));
            Commands.ChatCommands.Add(new Command("tshock.canchat", ChangeCommand, "change"));
        }

        private void ChangeCommand(CommandArgs args)
        {
            Player player = args.TPlayer;
            string classSelectErrorMsg = "Invalid class name! Usage: " + TShock.Config.CommandSpecifier + "change <className>\nClass List: " + string.Join(", ", ClassName);

            if (!(minx < player.Center.X / 16f && player.Center.X / 16f < maxx && miny < player.Center.Y / 16f && player.Center.Y / 16f < maxy)) //TODO: Use config, or Automaticary set from world info
            {
                #region ResetAndSetInventory

                if(args.Parameters.Count != 1 || args.Parameters[0] == null || args.Parameters[0] == "")
                {
                    args.Player.SendErrorMessage(classSelectErrorMsg);
                    return;
                }
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
                        Main.projectile[projIndex].active = false;
                        Main.projectile[projIndex].netUpdate = true;
                    }
                }

                string className = args.Parameters[0].ToLower();
                switch (className) // Ugly, but no idea
                {
                    case "standard":
                        {
                            player.inventory[0].SetDefaults(ItemID.IceBow);
                            player.inventory[0].prefix = PrefixID.Hurtful;
                            player.inventory[1].SetDefaults(ItemID.Musket);
                            player.inventory[2].SetDefaults(ItemID.EndlessQuiver);

                            player.inventory[3].SetDefaults(ItemID.MeteorShot);
                            player.inventory[3].stack = 999;
                            player.inventory[4].SetDefaults(ItemID.MeteorShot);
                            player.inventory[4].stack = 999;

                            player.armor[0].SetDefaults(ItemID.FrostHelmet);
                            player.armor[1].SetDefaults(ItemID.HuntressAltShirt);
                            player.armor[2].SetDefaults(ItemID.ShadowGreaves);
                            player.armor[10].SetDefaults(ItemID.HerosHat);
                            player.armor[11].SetDefaults(ItemID.HerosShirt);
                            player.armor[12].SetDefaults(ItemID.HerosPants);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Menacing;
                            player.armor[4].SetDefaults(ItemID.BlizzardinaBottle);
                            player.armor[4].prefix = PrefixID.Menacing;
                            player.armor[5].SetDefaults(ItemID.AnkletoftheWind);
                            player.armor[5].prefix = PrefixID.Menacing;
                            player.armor[6].SetDefaults(ItemID.RangerEmblem);
                            player.armor[6].prefix = PrefixID.Menacing;

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[5].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 200;
                            player.statManaMax = 20;

                            break;
                        }
                    case "killer":
                        {
                            player.inventory[0].SetDefaults(ItemID.SniperRifle);
                            player.inventory[0].prefix = PrefixID.Annoying;
                            player.inventory[1].SetDefaults(ItemID.EndlessMusketPouch);

                            player.armor[0].SetDefaults(ItemID.LeadHelmet);
                            player.armor[1].SetDefaults(ItemID.CrimsonScalemail);
                            player.armor[2].SetDefaults(ItemID.LeadGreaves);
                            player.armor[10].SetDefaults(ItemID.Sunglasses);
                            player.armor[11].SetDefaults(ItemID.TuxedoShirt);
                            player.armor[12].SetDefaults(ItemID.TuxedoPants);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Warding;
                            player.armor[4].SetDefaults(ItemID.CloudinaBottle);
                            player.armor[4].prefix = PrefixID.Warding;

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[2].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 200;
                            player.statManaMax = 20;

                            break;
                        }
                    case "gale":
                        {
                            player.armor[0].SetDefaults(ItemID.OrichalcumHelmet);
                            player.armor[1].SetDefaults(ItemID.Gi);
                            player.armor[2].SetDefaults(ItemID.HuntressPants);
                            player.armor[10].SetDefaults(ItemID.Goggles);

                            player.armor[3].SetDefaults(ItemID.BlueHorseshoeBalloon);
                            player.armor[3].prefix = PrefixID.Quick2;
                            player.armor[4].SetDefaults(ItemID.HermesBoots);
                            player.armor[4].prefix = PrefixID.Quick2;
                            player.armor[5].SetDefaults(ItemID.AnkletoftheWind);
                            player.armor[5].prefix = PrefixID.Quick2;
                            player.armor[6].SetDefaults(ItemID.Aglet);
                            player.armor[6].prefix = PrefixID.Quick2;
                            player.armor[7].SetDefaults(ItemID.Tabi);
                            player.armor[7].prefix = PrefixID.Quick2;
                            player.armor[8].SetDefaults(ItemID.PanicNecklace);
                            player.armor[8].prefix = PrefixID.Quick2;

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[0].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 200;
                            player.statManaMax = 20;

                            break;
                        }
                    case "hoplite":
                        {
                            player.inventory[0].SetDefaults(ItemID.IceBow);
                            player.inventory[0].prefix = PrefixID.Hurtful;
                            player.inventory[1].SetDefaults(ItemID.Musket);
                            player.inventory[2].SetDefaults(ItemID.EndlessQuiver);

                            player.inventory[3].SetDefaults(ItemID.MeteorShot);
                            player.inventory[3].stack = 999;
                            player.inventory[4].SetDefaults(ItemID.MeteorShot);
                            player.inventory[4].stack = 999;

                            player.armor[0].SetDefaults(ItemID.ChlorophyteMask);
                            player.armor[1].SetDefaults(ItemID.BeetleShell);
                            player.armor[2].SetDefaults(ItemID.VortexLeggings);
                            player.armor[10].SetDefaults(ItemID.GladiatorHelmet);
                            player.armor[11].SetDefaults(ItemID.GladiatorBreastplate);
                            player.armor[12].SetDefaults(ItemID.GladiatorLeggings);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Menacing;
                            player.armor[4].SetDefaults(ItemID.TsunamiInABottle);
                            player.armor[4].prefix = PrefixID.Menacing;
                            player.armor[5].SetDefaults(ItemID.PaladinsShield);
                            player.armor[5].prefix = PrefixID.Warding;
                            player.armor[6].SetDefaults(ItemID.WormScarf);
                            player.armor[6].prefix = PrefixID.Warding;
                            player.armor[7].SetDefaults(ItemID.FrozenTurtleShell);
                            player.armor[7].prefix = PrefixID.Warding;
                            player.armor[8].SetDefaults(ItemID.RangerEmblem);

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[5].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 160;
                            player.statManaMax = 20;

                            break;
                        }
                    case "creeper":
                        {
                            player.inventory[0].SetDefaults(ItemID.IceBow);
                            player.inventory[0].prefix = PrefixID.Hurtful;
                            player.inventory[1].SetDefaults(ItemID.Musket);
                            player.inventory[2].SetDefaults(ItemID.EndlessQuiver);

                            player.inventory[3].SetDefaults(ItemID.MeteorShot);
                            player.inventory[3].stack = 999;
                            player.inventory[4].SetDefaults(ItemID.MeteorShot);
                            player.inventory[4].stack = 999;

                            player.armor[1].SetDefaults(ItemID.SquireAltShirt);
                            player.armor[10].SetDefaults(ItemID.CreeperMask);
                            player.armor[11].SetDefaults(ItemID.CreeperShirt);
                            player.armor[12].SetDefaults(ItemID.CreeperPants);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Menacing;
                            player.armor[4].SetDefaults(ItemID.TsunamiInABottle);
                            player.armor[4].prefix = PrefixID.Menacing;
                            player.armor[5].SetDefaults(ItemID.CobaltShield);
                            player.armor[5].prefix = PrefixID.Menacing;
                            player.armor[6].SetDefaults(ItemID.ShinyStone);
                            player.armor[6].prefix = PrefixID.Menacing;
                            player.armor[7].SetDefaults(ItemID.BandofRegeneration);
                            player.armor[8].SetDefaults(ItemID.CharmofMyths);

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[5].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 400;
                            player.statManaMax = 20;

                            break;
                        }
                    case "ninja":
                        {
                            player.inventory[0].SetDefaults(ItemID.DartRifle);
                            player.inventory[0].prefix = PrefixID.Sluggish;
                            player.inventory[1].SetDefaults(ItemID.ChainGuillotines);
                            player.inventory[1].prefix = PrefixID.Ruthless;
                            player.inventory[2].SetDefaults(ItemID.PsychoKnife);
                            player.inventory[2].prefix = PrefixID.Ruthless;

                            player.armor[0].SetDefaults(ItemID.ShroomiteHelmet);
                            player.armor[1].SetDefaults(ItemID.ShroomiteBreastplate);
                            player.armor[2].SetDefaults(ItemID.ShroomiteLeggings);
                            player.armor[10].SetDefaults(ItemID.NinjaHood);
                            player.armor[11].SetDefaults(ItemID.NinjaShirt);
                            player.armor[12].SetDefaults(ItemID.NinjaPants);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[4].SetDefaults(ItemID.TsunamiInABottle);
                            player.armor[5].SetDefaults(ItemID.GravityGlobe);

                            player.inventory[4].SetDefaults(ItemID.CursedDart);
                            player.inventory[4].stack = 999;
                            player.inventory[5].SetDefaults(ItemID.CursedDart);
                            player.inventory[5].stack = 999;

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[3].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 140;
                            player.statManaMax = 20;

                            break;
                        }
                    case "desert":
                        {
                            player.inventory[0].SetDefaults(ItemID.OnyxBlaster);
                            player.inventory[0].prefix = PrefixID.Ruthless;
                            player.inventory[1].SetDefaults(ItemID.SkyFracture);
                            player.inventory[1].prefix = PrefixID.Ruthless;
                            player.inventory[2].SetDefaults(ItemID.SpiritFlame);
                            player.inventory[2].prefix = PrefixID.Inept;
                            player.inventory[3].SetDefaults(ItemID.EndlessMusketPouch);

                            player.armor[0].SetDefaults(ItemID.TitaniumHeadgear);
                            player.armor[1].SetDefaults(ItemID.ApprenticeRobe);
                            player.armor[2].SetDefaults(ItemID.NebulaLeggings);
                            player.armor[10].SetDefaults(ItemID.AncientArmorHat);
                            player.armor[11].SetDefaults(ItemID.AncientArmorShirt);
                            player.armor[12].SetDefaults(ItemID.AncientArmorPants);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Menacing;
                            player.armor[4].SetDefaults(ItemID.SandstorminaBottle);
                            player.armor[4].prefix = PrefixID.Menacing;
                            player.armor[5].SetDefaults(ItemID.FlyingCarpet);
                            player.armor[5].prefix = PrefixID.Menacing;
                            player.armor[6].SetDefaults(ItemID.AvengerEmblem);
                            player.armor[6].prefix = PrefixID.Menacing;
                            player.armor[7].SetDefaults(ItemID.SorcererEmblem);
                            player.armor[7].prefix = PrefixID.Menacing;
                            player.armor[8].SetDefaults(ItemID.DestroyerEmblem);
                            player.armor[8].prefix = PrefixID.Arcane;

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[4].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 180;
                            player.statManaMax = 20;
                            break;
                        }
                    case "wizard":
                        {
                            player.inventory[0].SetDefaults(ItemID.UnholyTrident);
                            player.inventory[0].prefix = PrefixID.Ruthless;
                            player.inventory[1].SetDefaults(ItemID.ToxicFlask);
                            player.inventory[1].prefix = PrefixID.Broken;
                            player.inventory[2].SetDefaults(ItemID.Flamelash);
                            player.inventory[2].prefix = PrefixID.Broken;

                            player.armor[0].SetDefaults(ItemID.TitaniumHeadgear);
                            player.armor[1].SetDefaults(ItemID.MeteorSuit);
                            player.armor[2].SetDefaults(ItemID.MeteorLeggings);
                            player.armor[10].SetDefaults(ItemID.WizardHat);
                            player.armor[11].SetDefaults(ItemID.Robe);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Menacing;
                            player.armor[4].SetDefaults(ItemID.CloudinaBottle);
                            player.armor[4].prefix = PrefixID.Menacing;
                            player.armor[5].SetDefaults(ItemID.SorcererEmblem);
                            player.armor[6].SetDefaults(ItemID.DestroyerEmblem);
                            player.armor[7].SetDefaults(ItemID.MagicCuffs);

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[3].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 180;
                            player.statManaMax = 20;
                            break;
                        }
                    case "frost":
                        {
                            player.inventory[0].SetDefaults(ItemID.IceSickle);
                            player.inventory[0].prefix = PrefixID.Ruthless;
                            player.inventory[1].SetDefaults(ItemID.FlowerofFrost);
                            player.inventory[1].prefix = PrefixID.Intense;
                            player.inventory[2].SetDefaults(ItemID.Amarok);
                            player.inventory[2].prefix = PrefixID.Damaged;

                            player.armor[0].SetDefaults(ItemID.FrostHelmet);
                            player.armor[1].SetDefaults(ItemID.FrostBreastplate);
                            player.armor[2].SetDefaults(ItemID.FrostLeggings);

                            player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                            player.armor[3].prefix = PrefixID.Arcane;
                            player.armor[4].SetDefaults(ItemID.BlizzardinaBottle);
                            player.armor[4].prefix = PrefixID.Arcane;
                            player.armor[5].SetDefaults(ItemID.WarriorEmblem);
                            player.armor[5].prefix = PrefixID.Arcane;
                            player.armor[6].SetDefaults(ItemID.WhiteString);
                            player.armor[6].prefix = PrefixID.Arcane;
                            player.armor[7].SetDefaults(ItemID.MechanicalGlove);
                            player.armor[7].prefix = PrefixID.Arcane;
                            player.armor[8].SetDefaults(ItemID.FireGauntlet);
                            player.armor[8].prefix = PrefixID.Arcane;

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[3].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 240;
                            player.statManaMax = 20;
                            break;
                        }
                    case "summon":
                        {
                            if(player.team == 3)
                            {
                                player.inventory[0].SetDefaults(ItemID.TempestStaff);
                                player.inventory[0].prefix = PrefixID.Shoddy;
                                player.inventory[1].SetDefaults(ItemID.DD2BallistraTowerT1Popper);
                                player.inventory[1].prefix = PrefixID.Demonic;

                                player.armor[0].SetDefaults(ItemID.ApprenticeHat);
                                player.armor[1].SetDefaults(ItemID.ApprenticeRobe);
                                player.armor[2].SetDefaults(ItemID.ApprenticeTrousers);

                                player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                                player.armor[3].prefix = PrefixID.Menacing;
                                player.armor[4].SetDefaults(ItemID.CloudinaBottle);
                                player.armor[4].prefix = PrefixID.Menacing;
                                player.armor[5].SetDefaults(ItemID.SummonerEmblem);
                                player.armor[5].prefix = PrefixID.Menacing;
                                player.armor[6].SetDefaults(ItemID.AvengerEmblem);
                                player.armor[6].prefix = PrefixID.Menacing;
                                player.armor[7].SetDefaults(ItemID.DestroyerEmblem);
                                player.armor[7].prefix = PrefixID.Menacing;
                                player.armor[8].SetDefaults(ItemID.MagicCuffs);
                                player.armor[8].prefix = PrefixID.Menacing;
                            }
                            else if (player.team == 4)
                            {
                                player.inventory[0].SetDefaults(ItemID.TempestStaff);
                                player.inventory[0].prefix = PrefixID.Shoddy;
                                player.inventory[1].SetDefaults(ItemID.QueenSpiderStaff);
                                player.inventory[1].prefix = PrefixID.Ruthless;

                                player.armor[0].SetDefaults(ItemID.ApprenticeHat);
                                player.armor[1].SetDefaults(ItemID.ApprenticeRobe);
                                player.armor[2].SetDefaults(ItemID.ApprenticeTrousers);

                                player.armor[3].SetDefaults(ItemID.LuckyHorseshoe);
                                player.armor[3].prefix = PrefixID.Menacing;
                                player.armor[4].SetDefaults(ItemID.CloudinaBottle);
                                player.armor[4].prefix = PrefixID.Menacing;
                                player.armor[5].SetDefaults(ItemID.SummonerEmblem);
                                player.armor[5].prefix = PrefixID.Menacing;
                                player.armor[6].SetDefaults(ItemID.AvengerEmblem);
                                player.armor[6].prefix = PrefixID.Menacing;
                                player.armor[7].SetDefaults(ItemID.DestroyerEmblem);
                                player.armor[7].prefix = PrefixID.Menacing;
                                player.armor[8].SetDefaults(ItemID.MagicCuffs);
                                player.armor[8].prefix = PrefixID.Menacing;
                            }

                            for (int i = 0; i < player.dye.Length; i++)
                            {
                                player.dye[i].SetDefaults(ItemID.TeamDye);
                            }
                            for (int i = 0; i < player.miscDyes.Length; i++)
                            {
                                player.miscDyes[i].SetDefaults(ItemID.TeamDye);
                            }

                            player.miscEquips[1].SetDefaults(ItemID.SuspiciousLookingTentacle);
                            player.armor[13].SetDefaults(ItemID.PartyBundleOfBalloonsAccessory);
                            player.inventory[2].SetDefaults(ItemID.MagicMirror);

                            player.statLifeMax = 180;
                            player.statManaMax = 20;
                            break;
                        }
                    default:
                        args.Player.SendErrorMessage(classSelectErrorMsg);
                        return;
                }
                #endregion

                if (player.statLife > player.statLifeMax)
                {
                    player.statLife = player.statLifeMax;
                }

                for (int i = 0; i < ClassName.Length; i++)
                {
                    if (ClassName[i] == className)
                    {
                        //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("index = " + args.Player.Index + " class : " + i), Color.White);
                        PlayerClass[args.Player.Index] = i;
                        break;
                    }
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
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, 0, 0, 0, 0, 0);
                    ++masterInvIndex;
                }
                for (int invIndex = 0; invIndex < NetItem.MiscEquipSlots; ++invIndex) // Hooks, Light Pet, etc...
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, 0, 0, 0, 0, 0);
                    ++masterInvIndex;
                }
                for (int invIndex = 0; invIndex < NetItem.MiscDyeSlots; ++invIndex)
                {
                    NetMessage.SendData(5, -1, -1, null, playerIndex, masterInvIndex, 0, 0, 0, 0, 0);
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

                SetBuffs(className, playerIndex);

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
