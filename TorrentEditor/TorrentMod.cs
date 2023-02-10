using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentMod
{
    public class Torrent
    {
        public static Action<string> Log;

        public static void WriteToFile(string sw, ItemBase item)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Open(sw, FileMode.Create, FileAccess.Write)))
            {
                EncodeData(writer, item);
            }
        }

        private static void WriteValue(BinaryWriter bw, string value)
        {
            var by = Encoding.UTF8.GetBytes(value);
            bw.Write(Encoding.UTF8.GetBytes($"{by.Length}:{value}"));
        }

        private static void WriteValue(BinaryWriter bw, char value)
        {
            bw.Write(value);
        }

        private static void WriteItem(BinaryWriter bw, StringItem value)
        {
            bw.Write(Encoding.UTF8.GetBytes($"{value.RawBytes.Length}:"));
            bw.Write(value.RawBytes);
        }

        private static void WriteItem(BinaryWriter bw, NumberItem value)
        {
            bw.Write(Encoding.UTF8.GetBytes($"i{value.NumberData}e"));
        }

        readonly static List<string> SKIP_PROP = new List<string>() {
            "announce-list", "publisher", "publisher-url", "comment", "source", "ttg_tag"
        };

        private static void EncodeData(BinaryWriter bw, ItemBase item)
        {
            if (item.ItemType == ItemType.Dictionary)
            {
                WriteValue(bw, 'd');
                foreach (var kv in (item as DictionaryItem).DictionaryData)
                {
                    switch (kv.Value.ItemType)
                    {
                        case ItemType.Dictionary:
                            WriteValue(bw, kv.Key);
                            EncodeData(bw, kv.Value);
                            break;
                        case ItemType.List:
                            if (SKIP_PROP.IndexOf(kv.Key.ToLower()) != -1)
                            {
                                Log?.Invoke($"跳过{kv.Key}");
                                continue;
                            }
                            WriteValue(bw, kv.Key);
                            EncodeData(bw, kv.Value);
                            break;
                        case ItemType.String:
                            if (SKIP_PROP.IndexOf(kv.Key.ToLower()) != -1)
                            {
                                Log?.Invoke($"跳过{kv.Key}");
                                continue;
                            }
                            WriteValue(bw, kv.Key);
                            if (kv.Key.ToLower() == "announce")
                            {
                                Log?.Invoke($"删除{kv.Key}");
                                WriteValue(bw, "");
                            }
                            else
                            {
                                WriteItem(bw, kv.Value as StringItem);
                            }
                            break;
                        case ItemType.Number:
                            WriteValue(bw, kv.Key);
                            WriteItem(bw, kv.Value as NumberItem);
                            break;
                    }
                }
                WriteValue(bw, 'e');
            }
            if (item.ItemType == ItemType.List)
            {
                WriteValue(bw, 'l');
                foreach (var i in (item as ListItem).ListData)
                {
                    switch (i.ItemType)
                    {
                        case ItemType.Dictionary:
                        case ItemType.List:
                            EncodeData(bw, i);
                            break;
                        case ItemType.String:
                            WriteItem(bw, i as StringItem);
                            break;
                        case ItemType.Number:
                            WriteItem(bw, i as NumberItem);
                            break;
                    }
                }
                WriteValue(bw, 'e');
            }
        }
        public static ItemBase DecodeFile(string sr)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(sr, FileMode.Open, FileAccess.Read)))
            {
                return DecodeData(reader);
            }
        }

        private static ItemBase DecodeData(BinaryReader br, Stack<bool> st = null)
        {
            var flag = br.PeekChar();
            List<byte> ls = new List<byte>();
            byte b = 0;
            switch (flag)
            {
                case 'e':
                    br.ReadByte();
                    return null;
                case 'l'://列表
                    br.ReadByte();
                    var itemLs = new ListItem();
                    ItemBase i = null;
                    if (st == null)
                    {
                        st = new Stack<bool>();
                    }
                    st.Push(true);
                    do
                    {
                        i = DecodeData(br, new Stack<bool>());
                        if (i != null)
                        {
                            itemLs.ListData.Add(i);
                        }
                        else
                        {
                            st.Pop();
                        }
                    } while (st.Count != 0 && br.BaseStream.Position != br.BaseStream.Length);

                    return itemLs;
                case 'd'://字典
                    br.ReadByte();
                    var itemDic = new DictionaryItem();
                    var key = DecodeData(br);
                    while (key != null && br.BaseStream.Position != br.BaseStream.Length)
                    {
                        var val = DecodeData(br);
                        itemDic.DictionaryData[(key as StringItem).StringData] = val;
                        key = DecodeData(br);
                    }

                    return itemDic;
                case 'i'://数字
                    br.ReadByte();
                    b = br.ReadByte();
                    while (b != 'e')
                    {
                        ls.Add(b);
                        b = br.ReadByte();
                    }
                    return new NumberItem(long.Parse(Encoding.UTF8.GetString(ls.ToArray()))) { RawBytes = ls.ToArray() };
                default://字符串
                    b = br.ReadByte();
                    while (b != ':')
                    {
                        ls.Add(b);
                        b = br.ReadByte();
                    }
                    var len = int.Parse(Encoding.UTF8.GetString(ls.ToArray()));
                    var bufStr = br.ReadBytes(len);
                    var data = Encoding.UTF8.GetString(bufStr);
                    return new StringItem(data) { RawBytes = bufStr };
            }
        }
    }

    public class ItemBase
    {
        public ItemType ItemType { get; set; }
        public byte[] RawBytes { get; set; }
    }

    public class StringItem : ItemBase
    {
        public StringItem(string data)
        {
            StringData = data;
            ItemType = ItemType.String;
        }
        public string StringData { get; set; }
    }
    public class NumberItem : ItemBase
    {
        public NumberItem(long num)
        {
            NumberData = num;
            ItemType = ItemType.Number;
        }
        public long NumberData { get; set; }
    }

    public class ListItem : ItemBase
    {
        public ListItem()
        {
            ItemType = ItemType.List;
        }
        public List<ItemBase> ListData { get; set; } = new List<ItemBase>();
    }

    public class DictionaryItem : ItemBase
    {
        public DictionaryItem()
        {
            ItemType = ItemType.Dictionary;
        }
        public Dictionary<string, ItemBase> DictionaryData { get; set; } = new Dictionary<string, ItemBase>();
    }

    public enum ItemType
    {
        String, Number, List, Dictionary
    }
}
