using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using Terraria;
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

        internal static readonly int[] PlayerClass = new int[256];

        internal static PvPConfig Config { get; set; }
        internal static readonly string PVP_CONFIG_PATH = Path.Combine(TShock.SavePath, "PvP_config.json");

        internal static readonly CultureInfo Culture = new CultureInfo("en-US");

        internal static readonly int GMCODE = (new Random()).Next(0, 999999);

        public PvPMain(Main game) : base(game)
        {

            Config = PvPConfig.Read(PVP_CONFIG_PATH);

            if (Config == null)
            {
                Console.WriteLine("Config is null.");
                Config = new PvPConfig();
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
            PvPHooks.Initialize(this);
        }

        protected override void Dispose(bool disposing)
        {
            PvPHooks.Dispose(disposing);
            base.Dispose(disposing);
        }
        #endregion

        //Couldn't find OnWorldLoaded hook
        private static bool mapChecked = false;
        private static List<PvPMap.Area> currentBlacklist = new List<PvPMap.Area>();
        private static List<PvPMap.Area> currentWhitelist = new List<PvPMap.Area>();
        internal static bool IsInStage (float playerX, float playerY)
        {
            if (!mapChecked)
            {
                foreach (var map in Config.Maps)
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

        internal static bool SetBuffs (int class_id, int player_index)
        {
            if (Config.Classes.Count <= class_id || class_id < 0)
            {
                return false;
            }

            int buff_count = Config.Classes[class_id].Buffs.Count;
            for (int i = 0; i < buff_count; i++)
            {
                Config.Classes[class_id].Buffs[i].Parse();
                TShock.Players[player_index].SetBuff(Config.Classes[class_id].Buffs[i].id, 216000);
            }
            return true;
        }

        internal static int CalcDamage(Player player, int damage, bool pvp = false, bool crit = false)
        {
			int defence = player.statDefense;

			double damageCalculated = Main.CalculatePlayerDamage(damage, defence);
			if (crit)
			{
				damageCalculated *= 2;
			}
            if (damageCalculated >= 1.0)
            {
                damageCalculated = (int)((1d - player.endurance) * damageCalculated);
                if (damageCalculated < 1.0)
                {
                    damageCalculated = 1.0;
                }

                // SolarFlare damage reduction here

                // Beetle armor damage reduction here

                // Paladin shield defence here
            }
			return (int)damageCalculated;
		}
    }
}
