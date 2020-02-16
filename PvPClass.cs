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
        public string ItemName { get; private set; } = "";
        
        [JsonProperty("prefix")]
        [DefaultValue("0")]
        public string PrefixName { get; private set; } = "0";
        
        [JsonProperty("stack")]
        [DefaultValue(1)]
        public int Stack { get; private set; } = 1;

        private bool need_parse = true;

        [JsonIgnore]
        public int ItemID { get; private set; }
        [JsonIgnore]
        public int Prefix { get; private set; }

        public void Parse()
        {
            //密結合だが、他に使う予定がない．
            if (!need_parse) return;
            //Item_IDから先に、全ての名前と衝突させる．数値ならそのまま使う．
            int id;
            if (Int32.TryParse(ItemName, out id))
            {
                ItemID = id;
            }
            else
            {
                var found_item = new List<Item>();
                if (string.IsNullOrEmpty(ItemName))
                {
                    ItemID = 0;
                }
                else
                {
                    Item item = new Item();
                    //大文字に正規化してから比較する．
                    string upperName = ItemName.ToUpperInvariant();

                    Main.player[Main.myPlayer] = new Player();
                    for (int i = 1; i < Main.maxItemTypes; i++)
                    {
                        item.netDefaults(i);
                        if (!string.IsNullOrWhiteSpace(item.Name))
                        {
                            if (item.Name.ToUpperInvariant() == upperName)
                            {
                                found_item = new List<Item> { item };
                                break;
                            }
                            //アイテムが一意に定まるならいいようにしてるけどしない方がいいかも．
                            if (item.Name.ToUpperInvariant().StartsWith(upperName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                found_item.Add(item.Clone());
                            }
                        }
                    }
                    if (found_item.Count == 1)
                    {
                        ItemID = found_item[0].type;
                    }
                    else
                    {
                        throw new Exception("Value Error, Item Name: " + ItemName);
                    }
                }
            }

            //prefixも大体同じ．
            id = 0;
            if (Int32.TryParse(PrefixName, out id))
            {
                Prefix = id;
            }
            else
            {
                const int FIRST_ITEM_PREFIX = 1;
                const int LAST_ITEM_PREFIX = 83;

                Item item = new Item();
                if (string.IsNullOrEmpty(PrefixName))
                {
                    Prefix = 0;
                }
                else
                {
                    item.SetDefaults(0);
                    string upperName = PrefixName.ToUpperInvariant();
                    var found_prefix = new List<int>();
                    for (int i = FIRST_ITEM_PREFIX; i <= LAST_ITEM_PREFIX; i++)
                    {
                        item.prefix = (byte)i;
                        string prefixName = item.AffixName().Trim().ToUpperInvariant();
                        if (prefixName == upperName)
                        {
                            found_prefix = new List<int>() { i };
                            break;
                        }
                        else if (prefixName.StartsWith(upperName, StringComparison.InvariantCultureIgnoreCase) == true)
                        {
                            found_prefix.Add(i);
                        }
                    }
                    if (found_prefix.Count == 1)
                    {
                        Prefix = found_prefix[0];
                    }
                }
            }
            need_parse = false;
        }
    }

    public class ArmorData : ItemData
    {
        [JsonProperty("armor_slot")]
        private string slot = "";

        [JsonIgnore]
        public int SlotID
        {
            get
            {
                switch (slot.ToUpperInvariant())
                {
                    case "HEAD":
                        return 0;
                    case "BODY":
                        return 1;
                    case "LEG":
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
        public int SlotID
        {
            get
            {
                switch (slot.ToUpperInvariant())
                {
                    case "PET":
                        return 0;
                    case "LIGHT_PET":
                        return 1;
                    case "MINECART":
                        return 2;
                    case "MOUNT":
                        return 3;
                    case "HOOK":
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
        public string BuffName { get; private set; }
        private bool need_parse = true;

        [JsonIgnore]
        public int id { get; private set; }

        public void Parse()
        {
            if (!need_parse) return;
            int id_;
            if(Int32.TryParse(BuffName, out id_))
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
                
                if (need_load)
                {
                    buff_names = new List<string>();
                    Type type = typeof(BuffID);
                    FieldInfo[] fields = type.GetFields();
                    foreach (FieldInfo field in fields)
                    {
                        buff_names.Add(field.Name.ToUpperInvariant());
                    }

                    need_load = false;
                }
                int count = buff_names.Count;
                for (int i = 0; i < count; i++)
                {
                    if (buff_names[i] == BuffName.ToUpperInvariant())
                    {
                        id = i + 1;
                        break;
                    }
                }
            }

            need_parse = false;
        }
    }

    public class PvPClass
    {
        [JsonProperty("id")]
        public int Id { get; private set; }

        [JsonProperty("class_name")]
        public string Name
        {
            get { return name; }
            private set { name = value.ToUpperInvariant(); }
        }
        private string name;
        
        [JsonProperty("team")]
        [DefaultValue("all")]
        private string team = "all";
        
        private bool need_parse = true;
        
        [JsonIgnore]
        public List<int> TeamIDs { get; private set; }

        [JsonProperty("items")]
        public List<ItemData> Items { get; private set; } = new List<ItemData>();

        [JsonProperty("ammos")]
        public List<ItemData> Ammos { get; private set; } = new List<ItemData>();
        [JsonProperty("armor")]
        public List<ArmorData> Armors { get; private set; } = new List<ArmorData>();

        [JsonProperty("accessory")]
        public List<ItemData> Accessorys { get; private set; } = new List<ItemData>();

        [JsonProperty("vanity_armor")]
        public List<ArmorData> VanityArmors { get; private set; } = new List<ArmorData>();

        [JsonProperty("vanity_accessory")]
        public List<ItemData> VanityAccessorys { get; private set; } = new List<ItemData>();

        [JsonProperty("buffs")]
        public List<BuffData> Buffs { get; private set; } = new List<BuffData>();

        [JsonProperty("armor_dye")]
        public List<ArmorData> ArmorDyes { get; private set; } = new List<ArmorData>();

        [JsonProperty("accessory_dye")]
        public List<ItemData> AccessoryDyes { get; private set; } = new List<ItemData>();

        [JsonProperty("misc_items")]
        public List<MiscData> MiscItems { get; private set; } = new List<MiscData>();

        [JsonProperty("misc_dye")]
        public List<MiscData> MiscDyes { get; private set; } = new List<MiscData>();

        [JsonProperty("max_hp")]
        [DefaultValue(100)]
        public int Hp { get; private set; }

        [JsonProperty("max_mp")]
        [DefaultValue(20)]
        public int Mp { get; private set; }

        public void Parse()
        {
            if (!need_parse) return;

            var list = new List<int>();
            var teams = team.Split(',');

            foreach (string team_ in teams)
            {
                int id;
                switch (team_.Trim().ToUpperInvariant())
                {
                    case "ALL":
                        TeamIDs = new List<int>() { 0, 1, 2, 3, 4, 5 };
                        need_parse = false;
                        return;
                    case "WHITE":
                        id = 0;
                        break;
                    case "RED":
                        id = 1;
                        break;
                    case "GREEN":
                        id = 2;
                        break;
                    case "BLUE":
                        id = 3;
                        break;
                    case "YELLOW":
                        id = 4;
                        break;
                    case "PINK":
                        id = 5;
                        break;
                    default:
                        id = -1;
                        break;
                }
                list.Add(id);
            }

            TeamIDs = list;
            need_parse = false;
        }
    }
}
