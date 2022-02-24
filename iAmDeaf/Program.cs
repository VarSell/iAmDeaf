using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Configuration;
using System.Threading;
using Newtonsoft.Json;
using AAXClean;

namespace Workings
{
    static class iAmDeaf
    {
        public const string mark = "iAmDeaf";
        public const string version = "1.4.2";
    }
    class Alert
    {
        public static void Notify(string alert)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(alert);
            Console.ResetColor();
            Console.WriteLine("]");
        }
        public static void Error(string alert)
        {
            Console.Write("  [Error: ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(alert);
            Console.ResetColor();
            Console.WriteLine("]");
        }
        public static void Success(string alert)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(alert);
            Console.ResetColor();
            Console.WriteLine("]");
        }
    }
    class Methods
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory.ToString();
        public static string nfo(string aax, string m4b)
        {
            string[] nfoPart = new string[15];
            try
            {
                string mi = $"{root}src\\tools\\mediainfo.exe";
                nfoPart[0] = SoftWare(mi, $"{aax} --Inform=General;%Album%", false);            //Title
                nfoPart[1] = SoftWare(mi, $"{aax} --Inform=General;%Performer%", false);        //Author
                nfoPart[2] = SoftWare(mi, $"{aax} --Inform=General;%nrt%", false);              //Narrator
                nfoPart[3] = SoftWare(mi, $"{aax} --Inform=General;%Copyright%", false);        //Copyright
                nfoPart[4] = SoftWare(mi, $"{aax} --Inform=General;%Genre%", false);            //Genre
                nfoPart[5] = SoftWare(mi, $"{aax} --Inform=General;%pub%", false);              //Publisher
                nfoPart[6] = SoftWare(mi, $"{aax} --Inform=General;%rldt%", false);             //Release Date
                nfoPart[7] = SoftWare(mi, $"{aax} --Inform=General;%Duration/String2%", false); //Duration (h, m)
                nfoPart[8] = SoftWare(mi, $"{aax} --Inform=\"Menu;%FrameCount%\"", false);      //Chapters
                nfoPart[9] = SoftWare(mi, $"{aax} --Inform=General;%Format%", false);           //general format
                nfoPart[10] = SoftWare(mi, $"{m4b} --Inform=Audio;%Format%", false);            //audio format
                nfoPart[11] = SoftWare(mi, $"{m4b} --Inform=Audio;%BitRate%", false).Trim();    //source bitrate
                try
                {
                    nfoPart[11] = (Int32.Parse(nfoPart[11]) / 1000).ToString();
                }
                catch
                {
                    nfoPart[11] = "NULL";
                    Alert.Error("Failed Getting Source BitRate");
                }
                nfoPart[12] = SoftWare(mi, $"{m4b} --Inform=General;%CodecID%", false); //encoded codecID
                nfoPart[13] = SoftWare(mi, $"{m4b} --Inform=Audio;%BitRate%", false);   //encoded bitrate
                try
                {
                    nfoPart[13] = (Int32.Parse(nfoPart[13]) / 1000).ToString();
                }
                catch
                {
                    nfoPart[11] = "NULL";
                    Alert.Error("Failed Getting Output Bitrate");
                }
                nfoPart[14] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false); //comment (Track_More)
            }
            catch (Exception ex)
            {
                Alert.Error($"NFO Failed: {ex.Message}");
            }

            string nfo = @$"General Information
===================
 Title:                  {nfoPart[0].Trim()}
 Author:                 {nfoPart[1].Trim()}
 Narrator:               {nfoPart[2].Trim()}
 AudioBook Copyright:    {nfoPart[3].Trim()}
 Genre:                  {nfoPart[4].Trim()}
 Publisher:              {nfoPart[5].Trim()}
 Published:              {nfoPart[6].Trim()}
 Duration:               {nfoPart[7].Trim()}
 Chapters:               {nfoPart[8].Trim()}

Media Information
=================
 Source Format:          Audible {nfoPart[9].Trim().ToUpper()} ({nfoPart[10].Trim()})
 Source Bitrate:         {nfoPart[11].ToString()} kbps

 Encoded Codec:          {nfoPart[12].Trim()}
 Encoded Bitrate:        {nfoPart[13].ToString()} kbps

Ripper:                  {iAmDeaf.mark} {iAmDeaf.version}

Publisher's Summary
===================
{nfoPart[14].Trim()}
";
            return nfo;
        }
        public static string[] MediaInfo(string aax)
        {
            aax = String.Concat("\"", aax, "\"");
            string mi = $"{root}src\\tools\\mediainfo.exe";
            string[] info = new string[5];
            info[0] = SoftWare(mi, $"{aax} --Inform=General;%Album%", false);
            info[1] = info[0].Split(",").Last().Trim();
            info[2] = info[0].Replace(info[1], null).Replace(",", null).Split(":").Last().Trim();
            info[3] = info[0].Replace(info[1], null).Replace(info[2], null).Replace(":", null).Replace(",", null).Trim();
            info[4] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false);
            info[1] = info[1].Replace(" ", null).Replace("Book", null);

            if (info[1].Contains("Volume"))
            {
                info[1] = info[1].Replace("Volume", "Volume ");
            }

            if (info[1].Length < 2)
            {
                info[1] = String.Concat("0", info[1]);
            }

            return info;
        }
        public static void CueClear()
        {
            if (File.Exists($"{root}src\\chapters.txt"))
            {
                File.Delete($"{root}src\\chapters.txt");
            }
            if (File.Exists($"{root}src\\chapters.cue"))
            {
                File.Delete($"{root}src\\chapters.cue");
            }
        }
        public static void cueGenerator(string aax, string file)
        {
            CueClear();
            string PID = Process.GetCurrentProcess().Id.ToString();
            SoftWare($@"{root}src\\tools\\ffmpeg.exe", $" -i \"{aax}\" -c copy {root}src\\data\\dump\\{PID}.mkv -y", true);
            SoftWare($@"{root}src\\tools\\mkvextract.exe", $" {root}src\\data\\dump\\{PID}.mkv chapters -s {root}src\\data\\dump\\{PID}.txt", true);
            string cuegen = $"{root}src\\tools\\cuegen.vbs {root}src\\data\\dump\\{PID}.txt";
            var CUEGEN = Process.Start(@"cmd", @"/c " + cuegen);
            CUEGEN.WaitForExit();
            CUEGEN.Close();
            CUEGEN.Dispose();

            string[] cue = File.ReadAllLines($"{root}src\\data\\dump\\{PID}.cue");
            cue[0] = $"FILE \"{Path.GetFileName($"{file}.m4b")}\" MP4";
            File.WriteAllLines($"{file}.cue", cue);

            if (!File.Exists($"{file}.cue"))
            {
                Alert.Error("CUE Failed");
            }
            else
            {
                Alert.Success("CUE Created");
            }

            File.Delete($"{root}src\\data\\dump\\{PID}.cue");
            File.Delete($"{root}src\\data\\dump\\{PID}.txt");
            File.Delete($"{root}src\\data\\dump\\{PID}.mkv");
        }

        public static void CreateAudioBook(string bytes, string aax, string file, string ext = "m4b")
        {
            if (!(ext == "m4b" || ext == "mp3"))
            {
                Alert.Notify("Invalid codec. Defaulting to M4B");
                ext = "m4b";
            }
            var aaxcFile = new AaxFile(File.OpenRead(aax));
            aaxcFile.SetDecryptionKey(bytes);

            try
            {
                aaxcFile.ConvertToMp4a(File.Open($"{file}.{ext}", FileMode.OpenOrCreate, FileAccess.ReadWrite));
                if (File.Exists($"{file}.{ext}"))
                {
                    Alert.Success("AudioBook Created");
                }
                else
                {
                    Alert.Error("AudioBook Creation Failed");
                }
            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
            }
        }
        public static string getBytes(string aax)
        {
            string currentSum = $"{root}\\src\\data\\KeyHistory\\CurrentSum";
            if (!File.Exists(currentSum))
            {
                File.Create(currentSum).Dispose();
            }
            string cacheBytes = $"{root}src\\data\\KeyHistory\\CurrentBytes";
            string checksum = SoftWare($@"{root}src\\tools\\ffprobe.exe", $"{aax}", true);
            File.WriteAllText(currentSum, checksum.Replace(" ", ""));
            string[] line = File.ReadAllLines(currentSum);
            checksum = (line[11].Split("==").Last());
            File.WriteAllText(currentSum, checksum);

            //  Search the log for checksum matches
            string[] keys = File.ReadAllLines(Path.Combine(root, @"src\data\KeyHistory\log"));
            for (int i = 0; i < keys.Length; i+=2)
            {
                if (keys[i] == checksum)
                {
                    Alert.Notify("Found Cached Bytes");
                    return keys[i+1];
                }
            }

            /*
             * Just as a reminder, this is where current dir is changed, as rcrack doesnt like to be launched when it's not in its root dir
             */

            Directory.SetCurrentDirectory($"{root}src\\tables");
            string bytes = SoftWare($"rcrack.exe", $" . -h {checksum}", false);
            File.WriteAllText(cacheBytes, bytes.Replace(" ", ""));
            line = File.ReadAllLines(cacheBytes);
            bytes = (line[32].Split("hex:").Last());
            File.WriteAllText(cacheBytes, bytes);
            Alert.Notify($"Act. Bytes: {bytes}");
            File.AppendAllText(Path.Combine(root, @"src\data\KeyHistory\log"), $"{checksum}\n{bytes}" + Environment.NewLine);
            return bytes;
        }
        public static string SoftWare(string software, string arguments, bool std)
        {
            Process SoftWare = new Process();
            SoftWare.StartInfo.FileName = @$"{software}";
            SoftWare.StartInfo.Arguments = $" {arguments} ";

            if (std == true)
            {
                SoftWare.StartInfo.RedirectStandardError = true;
                SoftWare.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                SoftWare.Start();

                using (StreamReader reader = SoftWare.StandardError)
                {
                    string result = reader.ReadToEnd();
                    SoftWare.WaitForExit();
                    return result;
                }
            }
            else
            {
                SoftWare.StartInfo.RedirectStandardOutput = true;
                SoftWare.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                SoftWare.Start();

                using (StreamReader reader = SoftWare.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    SoftWare.WaitForExit();
                    return result;
                }
            }
        }
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
            Console.CursorVisible = false;
            string aax = string.Empty;

            if (args.Length > 0)
            {

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

            string[] filename;
            string title, file, Output = string.Empty;
            bool CueEnabled = true;
            bool NfoEnabled = true;
            bool CoverEnabled = true;
            string hostDir = Path.GetDirectoryName(aax);
            Alert.Notify("Parsing File");

            //  Parsing of Json config.json settings
            Rootobject result = JsonConvert.DeserializeObject<Rootobject>(File.ReadAllText($"{root}\\src\\config.json"));
            string structure = $"{result.Title[0].T1} {result.Title[0].T2} {result.Title[0].T3} {result.Title[0].T4} {result.Title[0].T5}";
            structure = Regex.Replace(structure.Replace("null", null), @"\s+", " ").Trim();
            structure = structure.Replace(" ", " - ");

            try
            {
                if (result.DEFAULT == true)
                {
                    string Author = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Performer%", false).Trim();                                      //Author
                    string Title = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Album%", false).Trim().Replace(":", " -");                        //Title
                    string Year = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%rldt%", false).Trim();                                             //Year
                    Year = DateTime.ParseExact(Year, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("yyyy");
                    string Narrator = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%nrt%", false).Trim();                                          //Narrator
                    string Bitrate = (Int32.Parse(Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=Audio;%BitRate%", false).Trim()) / 1000).ToString() + "K";  //Bitrate            //Bitrate

                    file = structure.Replace("Author", Author);
                    file = file.Replace("Title", Title);
                    file = file.Replace("Year", Year);
                    file = file.Replace("Narrator", Narrator);
                    file = file.Replace("Bitrate", Bitrate);
                    Alert.Success(file);

                    Output = result.Output;
                    CueEnabled = result.Files[0].Cue;
                    NfoEnabled = result.Files[0].NFO;
                    CoverEnabled = result.Files[0].Cover;

                    System.IO.Directory.CreateDirectory($"{hostDir}\\{file}");
                    title = file;
                    file = $"{hostDir}\\{file}\\{file.Trim()}";
                }
                else
                {
                    filename = Workings.Methods.MediaInfo(aax);
                    title = filename[0];
                    filename[0] = filename[0].Trim().Replace(":", " -");
                    file = ($"{filename[2]} [{filename[1]}] {filename[3]}");
                    Alert.Success(file.Trim());
                    file = $"{hostDir}\\{filename[0]}\\{file.Trim()}";
                    System.IO.Directory.CreateDirectory($"{hostDir}\\{filename[0]}");
                }
            }
            catch
            {
                string info = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Album%", false);
                title = info;
                info = info.Trim().Replace(":", " -");
                file = info;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  {file}");
                Console.ResetColor();
                file = $"{hostDir}\\{info}\\{file.Trim()}";
                System.IO.Directory.CreateDirectory($"{hostDir}\\{info}");
            }

            string bytes = Workings.Methods.getBytes($"\"{aax}\"");

            Thread THR = new Thread(() => Workings.Methods.cueGenerator(aax, file));
            Thread THR1 = new Thread(() => Workings.Methods.CreateAudioBook(bytes, aax, file));
            THR1.Priority = ThreadPriority.AboveNormal;

            if (CueEnabled == true)
            {
                Alert.Notify("Generating CUE");
                THR.Start();
            }
            
            Alert.Notify("Creating AudioBook");
            THR1.Start();
            THR1.Join();

            if (CoverEnabled == true)
            {
                Alert.Notify("Extracting JPG");
                Workings.Methods.SoftWare($"{root}src\\tools\\ffmpeg.exe", $"-i \"{aax}\" -map 0:v -map -0:V -c copy \"{file}.jpg\" -y", true);
            }

            if ( CueEnabled == true)
            {
                THR.Join();
            }

            if (NfoEnabled == true)
            {
                string nfo = Workings.Methods.nfo($"\"{aax}\"", $"\"{file}.m4b\"");
                Alert.Notify("Generating NFO");
                File.WriteAllText($"{file}.nfo", nfo, Encoding.UTF8);
            }
            return 0;
        }
    }
    public class Rootobject
    {
        public bool DEFAULT { get; set; }
        public Title[] Title { get; set; }
        public Files[] Files { get; set; }
        public string Output { get; set; }
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


}