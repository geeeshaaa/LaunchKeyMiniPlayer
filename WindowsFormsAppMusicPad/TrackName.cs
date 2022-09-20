using System.IO;

namespace WindowsFormsAppMusicPad
{
    public class TrackName
    {
        public string path { get; set; }
        public string name { get; set; }
        public string ButtonName { get; set; }
        public int note { get; set; }

        public TrackName(string pth, string butName, int note)
        {
            path = pth; 
            name = Path.GetFileNameWithoutExtension(pth); 
            ButtonName = butName;
            this.note = note;
        }
        public override string ToString()
        {
            return $"{name}";
        }
    }
}
