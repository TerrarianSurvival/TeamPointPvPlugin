
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using System.ComponentModel;
using TerrariaApi.Server;

namespace TeamPointPvP
{
    public class ItemData
    {
        [JsonProperty("item_name")]
        string item_name = "";
        [JsonProperty("prefix")]
        [DefaultValue("0")]
        string prefix_name = "0";
        [JsonProperty("stack")]
        [DefaultValue(1)]
        public int stack = 1;
        private bool need_parse = true;

        [JsonIgnore]
        public int item_id { get; private set; }
        [JsonIgnore]
        public int prefix { get; private set; }

        public void Parse()
        {
            //密結合だが、他に使う予定がない．
            if (!need_parse) return;
            //Item_IDから先に、全ての名前と衝突させる．数値ならそのまま使う．
            int id;
            if (Int32.TryParse(item_name, out id))
            {
                item_id = id;
            }
            else
            {
                var found_item = new List<Item>();
                if (item_name == "")
                {
                    item_id = 0;
                }
                else
                {
                    Item item = new Item();
                    //小文字で比較する．
                    string nameLower = item_name.ToLowerInvariant();

                    Main.player[Main.myPlayer] = new Player();
                    for (int i = 1; i < Main.maxItemTypes; i++)
                    {
                        item.netDefaults(i);
                        if (!String.IsNullOrWhiteSpace(item.Name))
                        {
                            if (item.Name.ToLowerInvariant() == nameLower)
                            {
                                found_item = new List<Item> { item };
                                break;
                            }
                            if (item.Name.ToLowerInvariant().StartsWith(nameLower)) //アイテムが一意に定まるならいいようにしてるけどしない方がいいかも．
                            {
                                found_item.Add(item.Clone());
                            }
                        }
                    }
                    if (found_item.Count == 1)
                    {
                        item_id = found_item[0].type;
                    }
                    else
                    {
                        throw new Exception("Value Error, Item Name: " + item_name);
                    }
                }
            }

            //prefixも大体同じ．
            id = 0;
            if (Int32.TryParse(prefix_name, out id))
            {
                prefix = id;
            }
            else
            {
                const int FIRST_ITEM_PREFIX = 1;
                const int LAST_ITEM_PREFIX = 83;

                Item item = new Item();
                if (prefix_name == "")
                {
                    prefix = 0;
                }
                else
                {
                    item.SetDefaults(0);
                    string lowerName = prefix_name.ToLowerInvariant();
                    var found_prefix = new List<int>();
                    for (int i = FIRST_ITEM_PREFIX; i <= LAST_ITEM_PREFIX; i++)
                    {
                        item.prefix = (byte)i;
                        string prefixName = item.AffixName().Trim().ToLowerInvariant();
                        if (prefixName == lowerName)
                        {
                            found_prefix = new List<int>() { i };
                            break;
                        }
                        else if (prefixName.StartsWith(lowerName) == true)
                        {
                            found_prefix.Add(i);
                        }
                    }
                    if (found_prefix.Count == 1)
                    {
                        prefix = found_prefix[0];
                    }
                }
            }
            need_parse = false;
        }
    }

    public class ArmorData : ItemData
    {
        [JsonProperty("armor_slot")]
        string slot = "";

        [JsonIgnore]
        public int slot_id
        {
            get
            {
                switch (slot.ToLowerInvariant())
                {
                    case "head":
                        return 0;
                    case "body":
                        return 1;
                    case "leg":
                        return 2;
                    default:
                        return -1;
                }
            }
        } 
    }

    public class MiscData : ItemData
    {
        [JsonProperty("misc_slot")]
        string slot = "";

        [JsonIgnore]
        public int slot_id
        {
            get
            {
                switch (slot.ToLowerInvariant())
                {
                    case "pet":
                        return 0;
                    case "light_pet":
                        return 1;
                    case "minecart":
                        return 2;
                    case "mount":
                        return 3;
                    case "hook":
                        return 4;
                    default:
                        return -1;
                }
            }
        }
    }

    public class BuffData
    {
        static bool need_load = true;
        static List<string> buff_names = new List<string>();

        [JsonProperty("buff_name")]
        public string buff_name;
        private bool need_parse = true;

        [JsonIgnore]
        public int id { get; private set; }

        public void Parse()
        {
            if (!need_parse) return;
            int id_;
            if(Int32.TryParse(buff_name, out id_))
            {
                id = id_;
            }
            else
            {
                //(ﾋﾟﾛﾛﾛﾛﾛ…ｱｲｶﾞｯﾀﾋﾞﾘｨｰ)
                //
                //宝生永夢ゥ！ 何故君がBuffクラスを書かずにバフを実装できたのか
                //何故その効果を生み出せたのか(ｱﾛﾜﾅﾉｰ)
                //何故開発終了後に頭が痛むのくわァ！ (それ以上言うな！)
                //ﾜｲﾜｲﾜｰｲ その答えはただ一つ… (やめろー！)
                //ｱﾊｧｰ…♡
                //宝生永夢ゥ！君が世界で初めて…クソコードを書いた男だからだぁぁぁぁ！！
                //(ﾀｰﾆｯｫﾝ)アーハハハハハハハハハアーハハハハ(ｿｳﾄｳｴｷｻｰｲｴｷｻｰｲ)ハハハハハ！！！
                //
                //永夢「僕が……クソプログラマー……？」　ｯﾍｰｲ(煽り)
                //
                //BuffIDがあるのにBuffクラスがないってマジ？？？？？
                //
                if (need_load)
                {
                    buff_names = new List<string>();
                    Type type = typeof(BuffID);
                    FieldInfo[] fields = type.GetFields();
                    foreach (FieldInfo field in fields)
                    {
                        buff_names.Add(field.Name.ToLowerInvariant());
                    }

                    need_load = false;
                }
                int count = buff_names.Count;
                for (int i = 0; i < count; i++)
                {
                    if (buff_names[i] == buff_name.ToLowerInvariant())
                    {
                        id = i + 1;
                        break;
                    }
                }
                
                //if (id == 0)
                {
                    //Console.WriteLine(buff_name.ToLowerInvariant());
                    //Console.WriteLine(String.Join(",", buff_names));
                }
            }

            need_parse = false;
        }
    }

    public class PvPClass
    {
        [JsonProperty("id")]
        public int id;
        [JsonProperty("class_name")]
        public string name = "";
        [JsonProperty("team")]
        [DefaultValue("all")]
        string team = "all";
        private bool need_parse = true;
        [JsonIgnore]
        public List<int> team_id { get; private set; }

        [JsonProperty("items")]
        public List<ItemData> items = new List<ItemData>();
        [JsonProperty("ammos")]
        public List<ItemData> ammos = new List<ItemData>();
        [JsonProperty("armor")]
        public List<ArmorData> armors = new List<ArmorData>();
        [JsonProperty("accessory")]
        public List<ItemData> accessorys = new List<ItemData>();
        [JsonProperty("vanity_armor")]
        public List<ArmorData> vanity_armor = new List<ArmorData>();
        [JsonProperty("vanity_accessory")]
        public List<ItemData> vanity_accessorys = new List<ItemData>();

        [JsonProperty("buffs")]
        public List<BuffData> buffs;
        [JsonProperty("armor_dye")]
        public List<ArmorData> armor_dyes = new List<ArmorData>();
        [JsonProperty("accessory_dye")]
        public List<ItemData> accessory_dyes = new List<ItemData>();
        [JsonProperty("misc_items")]
        public List<MiscData> misc_items = new List<MiscData>();
        [JsonProperty("misc_dye")]
        public List<MiscData> misc_dyes = new List<MiscData>();
        [JsonProperty("max_hp")]
        [DefaultValue(100)]
        public int hp = 100;
        [JsonProperty("max_mp")]
        [DefaultValue(20)]
        public int mp = 20;

        public void Parse()
        {
            if (!need_parse) return;

            var list = new List<int>();
            var teams = team.Split(',');

            foreach (string team_ in teams)
            {
                int id;
                switch (team_.Trim().ToLowerInvariant())
                {
                    case "all":
                        team_id = new List<int>() { 0, 1, 2, 3, 4, 5 };
                        need_parse = false;
                        return;
                    case "white":
                        id = 0;
                        break;
                    case "red":
                        id = 1;
                        break;
                    case "green":
                        id = 2;
                        break;
                    case "blue":
                        id = 3;
                        break;
                    case "yellow":
                        id = 4;
                        break;
                    case "pink":
                        id = 5;
                        break;
                    default:
                        id = -1;
                        break;
                }
                list.Add(id);
            }

            team_id = list;
            need_parse = false;
        }
    }
}
