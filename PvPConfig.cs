using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TeamPointPvP
{
    class PvPConfig
    {
        [JsonProperty("classes")]
        public List<PvPClass> classes = new List<PvPClass>();

        [JsonProperty("maps")]
        public List<PvPMap> maps = new List<PvPMap>();

        internal static PvPConfig Read(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("PvP_class_config: Using default configs");
                PvPConfig config = new PvPConfig();
                config.Write(path);
                return config;
            }
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        internal static PvPConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<PvPConfig>(sr.ReadToEnd());
            }
        }

        internal void Write(string path)
        {
            string dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        internal void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }
    }

    class PvPMap
    {
        public class Area
        {
            public int minX;
            public int minY;
            public int maxX;
            public int maxY;

            public bool ContainsPoint(Vector2 point)
            {
                return ContainsPoint(point.X, point.Y);
            }

            public bool ContainsPoint(float x, float y)
            {
                return minX <= x && minY <= y && x <= maxX && y <= maxY;
            }
        }

        [JsonProperty("name")]
        public string name;

        [JsonProperty("blacklist")]
        public List<Area> blacklist = new List<Area>();

        [JsonProperty("whitelist")]
        public List<Area> whitelist = new List<Area>();
    }
}
