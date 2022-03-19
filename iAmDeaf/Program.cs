using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using Files;
using static Other;


namespace Workings
{
    public class iAmDeaf
    {
        public const string mark = "iAmDeaf";
        public const string version = "2.0.0";
    }
}

namespace Main
{
    using Workings;
    class Program
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;
        static int Main(string[] args)
        {
            string aax = string.Empty;

            if (args.Length > 0)
            {
                if (args[0] == "-c")
                {
                    try
                    {
                        Alert.Notify($"AAXC Decryption: {args[1]}");
                        Plus.Catagolue.Download(args[1]);
                    }
                    catch (Exception ex)
                    {
                        Alert.Error(ex.Message);
                        Alert.Error("Missing ASIN");
                        Alert.Notify("Usage: iAmDeaf -c <ASIN>");
                    }

                    return 0;
                }

                // AAXC AAX DIVIDER

                foreach (Object obj in args)
                {
                    aax = obj.ToString();
                }

                if (!File.Exists(aax))
                {
                    Alert.Error("Invalid filename.");
                    return 0;
                }
            }

            else
            {
                Alert.Error("No args provided.");
                return 0;
            }

            // Local AAXC check
            if (Path.GetExtension(aax) == ".aaxc")
            {
                PlusLocal.LocalAAXC.GetPaths(aax);
                return 0;
            }


            Console.CursorVisible = false;

            string[] filename;
            string title, file = string.Empty;
            string Codec = "m4b";
            bool CueEnabled = true;
            bool NfoEnabled = true;
            bool CoverEnabled = true;
            bool Split = false;
            string hostDir = Path.GetDirectoryName(aax);

            Alert.Notify("Parsing File");

            
            Rootobject Settings = JsonConvert.DeserializeObject<Rootobject>(File.ReadAllText($"{root}\\src\\config.json"));
            string structure = $"{Settings.Title[0].T1} {Settings.Title[0].T2} {Settings.Title[0].T3} {Settings.Title[0].T4} {Settings.Title[0].T5}";
            structure = Regex.Replace(structure.Replace("null", null), @"\s+", " ").Trim();
            structure = structure.Replace(" ", " - ");

            try
            {
                if (Settings.DEFAULT)
                {
                    string Author = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Performer%", false).Trim();                                      //Author       
                    string Title = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Album%", false).Trim().Replace(":", " -");                        //Title         
                    string Year = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%rldt%", false).Trim();                                             //Year        
                    Year = DateTime.ParseExact(Year, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("yyyy");          
                    string Narrator = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%nrt%", false).Trim();                                          //Narrator         
                    string Bitrate = (Int32.Parse(SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=Audio;%BitRate%", false).Trim()) / 1000).ToString() + "K";  //Bitrate            //Bitrate

                    file = structure.Replace("Author", Author);
                    file = file.Replace("Title", Title);
                    file = file.Replace("Year", Year);
                    file = file.Replace("Narrator", Narrator);
                    file = file.Replace("Bitrate", Bitrate);

                    Alert.Success(file);

                    Codec = Settings.Output[0].Codec;
                    Split = Settings.Output[0].Split;
                    CueEnabled = Settings.Files[0].Cue;
                    NfoEnabled = Settings.Files[0].NFO;
                    CoverEnabled = Settings.Files[0].Cover;

                    System.IO.Directory.CreateDirectory($"{hostDir}\\{file}");
                    title = file;
                    file = $"{hostDir}\\{file}\\{file.Trim()}";
                }
                else
                {
                    filename = Get.AaxInformation(aax);
                    title = filename[0];
                    filename[0] = filename[0].Trim().Replace(":", " -");
                    file = ($"{filename[2]} [{filename[1]}] {filename[3]}");
                    Alert.Success(file.Trim());
                    var _file = file;
                    file = $"{hostDir}\\{file.Trim()}\\{file.Trim()}";
                    System.IO.Directory.CreateDirectory($"{hostDir}\\{_file.Trim()}");
                }
            }
            catch
            {
                string info = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Album%", false);
                title = info;
                info = info.Trim().Replace(":", " -");
                file = info;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  {file}");
                Console.ResetColor();
                file = $"{hostDir}\\{info}\\{file.Trim()}";
                System.IO.Directory.CreateDirectory($"{hostDir}\\{info}");
            }

            string format = string.Empty; ;
            switch (Codec)
            {
                case "m4b": format = "MP4"; break;
                case "mp3": format = "MP3"; break;
            }

            string bytes = Get.ActivationBytes(aax);
            if (bytes == string.Empty)
            {
                return 0;
            }
            Stopwatch sw = Stopwatch.StartNew();
            Thread THR = new Thread(() => Create.Cue(aax, file, Codec, format));
            Thread THR1 = new Thread(() => Create.AudioBook(bytes, aax, file, Codec, Split));
            THR1.Priority = ThreadPriority.AboveNormal;

            Thread Lavf_Monitor = new Thread(() => Get.Monitor(String.Concat(@"src\data\dump\", Process.GetCurrentProcess().Id, $".{Codec}")));


            if (CueEnabled == true)
            {
                Alert.Notify("Generating Cue");
                THR.Start();
            }

            Alert.Notify("Creating AudioBook");
            THR1.Start();

            if (Codec != "m4b" && !Split)
            {
                Lavf_Monitor.Start();
                Lavf_Monitor.Join();
            }
            THR1.Join();
            

            if (CoverEnabled == true)
            {
                Alert.Notify("Extracting JPG");
                SoftWare($"{root}src\\tools\\ffmpeg.exe", $"-i \"{aax}\" -map 0:v -map -0:V -c copy \"{file}.jpg\" -y", true);
            }

            if (CueEnabled == true)
            {
                THR.Join();
            }

            if (NfoEnabled == true)
            {
                string nfo;

                if (!Split)
                {
                    Alert.Notify("Generating NFO");
                    nfo = Create.nfo(aax, $"{file}.{Codec}");
                }
                else
                {
                    string[] extensions = { string.Concat('.', Codec) };
                    var files = Directory.GetFiles(Path.GetDirectoryName(file), ".").Where(f => Array.Exists(extensions, e => f.EndsWith(e))).ToArray();
                    Alert.Notify("Generating NFO");
                    nfo = Create.nfo(aax, files[0], Split);
                }

                File.WriteAllText($"{file}.nfo", nfo, Encoding.UTF8);
            }

            sw.Stop();
            Alert.Notify($"Execution: {(sw.ElapsedMilliseconds / 1000).ToString()}s");

            Console.CursorVisible = true;
            return 0;
        }
    }

    public class Rootobject
    {
        public bool DEFAULT { get; set; }
        public Title[] Title { get; set; }
        public Files[] Files { get; set; }
        public Output[] Output { get; set; }
    }

    public class Title
    {
        public string T1 { get; set; }
        public string T2 { get; set; }
        public string T3 { get; set; }
        public string T4 { get; set; }
        public string T5 { get; set; }
    }

    public class Files
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

    
}