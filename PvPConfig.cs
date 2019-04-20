using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamPointPvP
{
    class PvPConfig
    {
        [JsonProperty("classes")]
        public List<PvPClass> classes = new List<PvPClass>();
        internal static PvPConfig Read(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Using default configs");
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
}
