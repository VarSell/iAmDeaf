namespace iAmDeaf.Config
{
    public class Settings
    {
        public bool DEFAULT { get; set; }
        public Title[] Title { get; set; }
        public File[] Files { get; set; }
        public Output[] Output { get; set; }
        public AAXC[] AAXC { get; set; }
    }

    public class Title
    {
        public string T1 { get; set; }
        public string T2 { get; set; }
        public string T3 { get; set; }
        public string T4 { get; set; }
        public string T5 { get; set; }
    }

    public class File
    {
        public bool Cue { get; set; }
        public bool NFO { get; set; }
        public bool Cover { get; set; }
    }

    public class Output
    {
        public string Codec { get; set; }
        public bool Split { get; set; }
    }

    public class AAXC
    {
        public bool NFO { get; set; }
        public bool Cue { get; set; }
        public bool Cover { get; set; }
        public bool Split { get; set; }
        public bool Backup { get; set; }
    }

}
