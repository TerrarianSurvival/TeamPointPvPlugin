using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TeamPointPvP
{
    class PvPConfig
    {
        private const string USE_DEFAULT_CONFIG_MESSAGE = "PvP_class_config: Using default configs";

        [JsonProperty("classes")]
        public List<PvPClass> Classes { get; private set; } = new List<PvPClass>();

        [JsonProperty("maps")]
        public List<PvPMap> Maps { get; private set; } = new List<PvPMap>();

        internal static PvPConfig Read(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine(USE_DEFAULT_CONFIG_MESSAGE);
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

    internal class PvPMap
    {
        public class Area
        {
            public int MinX { get; }
            public int MinY { get; }
            public int MaxX { get; }
            public int MaxY { get; }

            public Area(int minX, int minY, int maxX, int maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            public bool ContainsPoint(Vector2 point)
            {
                return ContainsPoint(point.X, point.Y);
            }

            public bool ContainsPoint(float x, float y)
            {
                return MinX <= x && MinY <= y && x <= MaxX && y <= MaxY;
            }
        }

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("blacklist")]
        public List<Area> BlackList { get; private set; } = new List<Area>();

        [JsonProperty("whitelist")]
        public List<Area> WhiteList { get; private set; } = new List<Area>();
    }
}
