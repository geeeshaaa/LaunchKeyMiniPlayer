using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace WindowsFormsAppMusicPad
{
    class SaveLoad
    {
        private readonly string PATH;
        public SaveLoad(string path) { PATH = path; }

        public List<TrackName> Load()
        {
            var fileExists = File.Exists(PATH);
            if (!fileExists)
            {
                File.CreateText(PATH).Dispose();
                return new List<TrackName>();
            }
            using (var reader = File.OpenText(PATH))
            {
                var fileText = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<List<TrackName>>(fileText);
            }
        }

        public void Save(object obj)
        {
            using (StreamWriter writen = File.CreateText(PATH))
            {
                string output = JsonConvert.SerializeObject(obj);
                writen.Write(output);
            }
        }
    }
}
