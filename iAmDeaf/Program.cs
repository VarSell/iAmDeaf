using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Configuration;
using System.Threading;


namespace Workings
{
    static class iAmDeaf
    {
        public const string mark = "iAmDeaf";
        public const string version = "1.4.1";
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
                nfoPart[0] = SoftWare(mi, $"{aax} --Inform=General;%Album%", false); //Title
                nfoPart[1] = SoftWare(mi, $"{aax} --Inform=General;%Performer%", false); //Author
                nfoPart[2] = SoftWare(mi, $"{aax} --Inform=General;%nrt%", false); //Narrator
                nfoPart[3] = SoftWare(mi, $"{aax} --Inform=General;%Copyright%", false); //Copyright
                nfoPart[4] = SoftWare(mi, $"{aax} --Inform=General;%Genre%", false); //Genre
                nfoPart[5] = SoftWare(mi, $"{aax} --Inform=General;%pub%", false); //Publisher
                nfoPart[6] = SoftWare(mi, $"{aax} --Inform=General;%rldt%", false); //Release Date
                nfoPart[7] = SoftWare(mi, $"{aax} --Inform=General;%Duration/String2%", false); //Duration (h, m)
                nfoPart[8] = SoftWare(mi, $"{aax} --Inform=\"Menu;%FrameCount%\"", false); //Chapters
                nfoPart[9] = SoftWare(mi, $"{aax} --Inform=General;%Format%", false); //general format
                nfoPart[10] = SoftWare(mi, $"{m4b} --Inform=Audio;%Format%", false); //audio format
                nfoPart[11] = SoftWare(mi, $"{m4b} --Inform=Audio;%BitRate%", false).Trim(); //source bitrate
                try
                {
                    nfoPart[11] = (Int32.Parse(nfoPart[11]) / 1000).ToString();
                }
                catch
                {
                    nfoPart[11] = "125";
                    AlertError("nfo Source Bitrate ERROR");
                }
                nfoPart[12] = SoftWare(mi, $"{m4b} --Inform=General;%CodecID%", false); //encoded codecID
                nfoPart[13] = SoftWare(mi, $"{m4b} --Inform=Audio;%BitRate%", false); //encoded bitrate
                try
                {
                    nfoPart[13] = (Int32.Parse(nfoPart[13]) / 1000).ToString();
                }
                catch
                {
                    nfoPart[11] = "125";
                    AlertError("nfo Output Bitrate ERROR");
                }
                nfoPart[14] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false); //comment
            }
            catch (Exception ex)
            {
                AlertError("nfo ERROR");
                AlertError(ex.Message);
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

            if (info[1].Length > 1)
            {
                info[1] = info[1];
            }
            else
            {
                info[1] = String.Concat("0", info[1]);
            }
            return info;
        }
        
        public static void Alert(string alert)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(alert);
            Console.ResetColor();
            Console.WriteLine("]");
        }

        public static void AlertError(string alert)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(alert);
            Console.ResetColor();
            Console.WriteLine("]");
        }

        public static void AlertSuccess(string alert)
        {
            Console.Write("  [");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(alert);
            Console.ResetColor();
            Console.WriteLine("]");
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

        public static void cueGenTHR(string aax, string file)
        {
            CueClear();
            SoftWare($@"{root}src\\tools\\ffmpeg.exe", $" -i \"{aax}\" -c copy {root}src\\data\\temp.mkv -y", true);
            SoftWare($@"{root}src\\tools\\mkvextract.exe", $" {root}src\\data\\temp.mkv chapters -s {root}src\\data\\chapters.txt", true);
            File.Delete($"{root}src\\data\\temp.mkv");
            string cuegen = $"{root}src\\tools\\cuegen.vbs {root}src\\data\\chapters.txt";
            Process.Start(@"cmd", @"/c " + cuegen);
            Thread.Sleep(700); //1000 is advised
            string[] cue = File.ReadAllLines($"{root}src\\data\\chapters.cue");
            cue[0] = $"FILE \"{Path.GetFileName($"{file}.m4b")}\" MP4";
            File.WriteAllLines($"{file.Replace("\"", "")}.cue", cue);
            if (!File.Exists($"{file.Replace("\"", "")}.cue"))
            {
                AlertError("CUE Error");
            }
            else
            {
                AlertSuccess("CUE Success");
            }
        }

        public static void m4bMuxer(string bytes, string aax, string title, string comment, string file)
        {
            SoftWare($"{root}src\\tools\\ffmpeg.exe", $"-activation_bytes {bytes} -i {aax} -metadata:g encoding_tool=\"{Workings.iAmDeaf.mark} {Workings.iAmDeaf.version}\" -metadata title=\"{title}\" -metadata comment=\"{comment}\" -c copy \"{file}.m4b\" -y", true);
            if (!File.Exists($"{file}.m4b"))
            {
                AlertError("M4B Error");
            }
            else
            {
                AlertSuccess("M4B Success");
            }
        }

        public static string getBytes(string aax)
        {
            string currentSum = $"{root}\\src\\data\\checksum.txt";
            if (!File.Exists(currentSum))
            {
                File.Create(currentSum).Dispose();
            }
            string cacheSum = File.ReadAllText(currentSum);
            string cacheBytes = $"{root}src\\data\\bytes.txt";
            string checksum = SoftWare($@"{root}src\\tools\\ffprobe.exe", $"{aax}", true);
            File.WriteAllText(currentSum, checksum.Replace(" ", ""));
            string[] line = File.ReadAllLines(currentSum);
            checksum = (line[11].Split("==").Last());
            File.WriteAllText(currentSum, checksum);

            Alert($"Checksum: {checksum}");

            


            if (cacheSum == checksum)
            {
                Alert("Checksum Match");
                Alert("Using Cached Bytes");
                return File.ReadAllText(cacheBytes);
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
            Alert($"Bytes: {bytes}");
            return bytes;
        }

        public static void Monitor(string m4b)
        {
            m4b = String.Concat(m4b, ".m4b");
            decimal buffer;
            Thread.Sleep(700);
            while (true)
            {
                buffer = (decimal)new System.IO.FileInfo(m4b).Length;
                Console.Write($"  {Decimal.Round(buffer / 1000000, 2)} MB");
                Console.Write("\r");
                Thread.Sleep(78);
                if (buffer == (new System.IO.FileInfo(m4b).Length))
                {
                    AlertSuccess($"{Decimal.Round(buffer / 1000000, 2)} MB");
                    break;
                }
            }
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
    class Program
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory.ToString();

        static int Main(string[] args)
        {
            Console.CursorVisible = false;
            string aax = null;

            if (args.Length > 0)
            {

                foreach (Object obj in args)
                {
                    aax = obj.ToString();

                    aax = $"\"{aax}\"";
                }
                
                if (!File.Exists(aax.Replace("\"", "")))
                {
                    Workings.Methods.AlertError("Invalid filename.");
                    return 0;
                }
            }

            else
            {
                Workings.Methods.AlertError("No args provided.");
                return 0;
            }

            string[] filename;
            string comment;
            string title;
            string file;
            string hostDir = Path.GetDirectoryName(aax).Replace("\"", "");

            Workings.Methods.Alert("Parsing File");
            try
            {
                filename = Workings.Methods.MediaInfo(aax);
                title = filename[0];
                filename[0] = filename[0].Trim().Replace(":", " -");
                file = ($@"{filename[2]} [{filename[1]}] {filename[3]}");
                /*Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  {file}");
                Console.ResetColor();*/
                Workings.Methods.AlertSuccess(file.Trim());
                comment = filename[4].Trim();
                file = $"\"{hostDir}\\{filename[0]}\\{file.Trim()}";
                System.IO.Directory.CreateDirectory($"{hostDir}\\{filename[0]}");
            }
            catch (Exception ex)
            {
                string info = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"{aax} --Inform=General;%Album%", false);
                title = info;
                info =info.Trim().Replace(":", " -");
                file = info;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  {file}");
                Console.ResetColor();
                comment = Workings.Methods.SoftWare($"{root}src\\tools\\mediainfo.exe", $"{aax} --Inform=General;%Track_More%", false);
                file = $"\"{hostDir}\\{info}\\{file.Trim()}";
                System.IO.Directory.CreateDirectory($"{hostDir}\\{info}");
            }
            
            string bytes = Workings.Methods.getBytes(aax);

            Thread buffer = new Thread(() => Workings.Methods.Monitor(file.Replace("\"", "")));
            Thread THR = new Thread(() => Workings.Methods.cueGenTHR($"{aax.Replace("\"", "")}", $"{file.Replace("\"", "")}"));
            Thread THR1 = new Thread(() => Workings.Methods.m4bMuxer(bytes, aax, title, comment.Replace("\"", ""), file.Replace("\"", "")));

            Workings.Methods.Alert("Generating CUE");
            THR.Start();
            Workings.Methods.Alert("Muxing M4B");
            THR1.Start();
            buffer.Start();
            buffer.Join();

            THR1.Join();

            Workings.Methods.Alert("Extracting JPG");
            Workings.Methods.SoftWare($"{root}src\\tools\\ffmpeg.exe", $"-i {aax} -map 0:v -map -0:V -c copy {file}.jpg\" -y", true);
            string nfo = Workings.Methods.nfo(aax, $"{file}.m4b\"");

            THR.Join();

            Workings.Methods.Alert("Generating NFO");
            File.WriteAllText(($"{file}.nfo\"").Replace("\"", ""), nfo, Encoding.UTF8);

            return 0;
        }
    }
}
