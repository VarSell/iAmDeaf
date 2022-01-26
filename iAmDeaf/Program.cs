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

namespace Workings
{

    class Methods
    {

        public static string nfo(string aax, string m4b)
        {

            string[] nfoPart = new string[15];
            string mi = "src\\tools\\mediainfo.exe";
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
            nfoPart[11] = SoftWare(mi, $"{m4b} --Inform=Audio;%BitRate%", false); //source bitrate
            nfoPart[11] = nfoPart[11].Substring(0, nfoPart[11].Length - 5);
            nfoPart[12] = SoftWare(mi, $"{m4b} --Inform=General;%CodecID%", false); //encoded codecID
            nfoPart[13] = SoftWare(mi, $"{m4b} --Inform=Audio;%BitRate%", false); //encoded bitrate
            nfoPart[13] = nfoPart[13].Substring(0, nfoPart[13].Length - 5);
            nfoPart[14] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false); //comment


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
 Source Format:          Audible {nfoPart[9].Trim()} ({nfoPart[10].Trim()})
 Source Bitrate:         {nfoPart[11].Trim()} kbps

 Encoded Codec:          {nfoPart[12].Trim()}
 Encoded Bitrate:        {nfoPart[13].Trim()} kbps

Ripper:                  iAmDeaf 1.0

Publisher's Summary
===================
{nfoPart[14].Trim()}
";

            return nfo;
        }

        public static string[] MediaInfo(string aax)
        {
            string mi = "src\\tools\\mediainfo.exe";
            string[] info = new string[5];

            info[0] = SoftWare(mi, $"{aax} --Inform=General;%Album%", false);
            info[1] = info[0].Split(",").Last().Trim();
            info[2] = info[0].Replace(info[1], null).Replace(",", null).Split(":").Last().Trim();
            info[3] = info[0].Replace(info[1], null).Replace(info[2], null).Replace(":", null).Replace(",", null).Trim();
            info[4] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false);

            info[1] = info[1].Replace(" ", null).Replace("Book", null);
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{alert}]");
            Console.ResetColor();
        }

        public static string getBytes(string aax)
        {

            string checksum = SoftWare($@"{Directory.GetCurrentDirectory()}\\src\\tools\\ffprobe.exe", $"{aax}", true);
            File.WriteAllText("src\\data\\checksum.txt", checksum.Replace(" ", ""));
            string[] line = File.ReadAllLines("src\\data\\checksum.txt");
            checksum = (line[11].Split("==").Last());
            File.WriteAllText("src\\data\\checksum.txt", checksum);

            Alert($"Checksum: {checksum}");

            string bytes = SoftWare($@"{Directory.GetCurrentDirectory()}\\src\\tables\\rcrack.exe", $" . -h {checksum}", false);
            File.WriteAllText("src\\data\\bytes.txt", bytes.Replace(" ", ""));
            line = File.ReadAllLines("src\\data\\bytes.txt");
            bytes = (line[32].Split("hex:").Last());
            File.WriteAllText("src\\data\\bytes.txt", bytes);

            Alert($"ActBytes: {bytes}");

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
    class Program
    {
        static int Main(string[] args)
        {
            string aax = null;

            if (args.Length > 0)
            {

                foreach (Object obj in args)
                {
                    aax = obj.ToString();
                    aax = $"\"{aax}\"";
                }
            }

            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No command line arguments found.");
                Console.ResetColor();
                return 0;
            }


            

            Workings.Methods.Alert("Parsing File");



            string bytes = Workings.Methods.getBytes(aax);

            Workings.Methods.Alert("Writing..");
            string[] filename = Workings.Methods.MediaInfo(aax);
            string title = filename[0];
            filename[0] = filename[0].Trim().Replace(":", " -");
            //System.IO.Directory.CreateDirectory(filename[0]);
            string file = $@"{filename[2]} [{filename[1]}] {filename[3]}";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(file);
            Console.ResetColor();
            string comment = filename[4].Trim();


            //*** create AAX Host dir HERE before ffmpeg merge

            string hostDir = Path.GetDirectoryName(aax).Replace("\"", "");
            file = $"\"{hostDir}\\{filename[0]}\\{file}";

            System.IO.Directory.CreateDirectory($"{hostDir}\\{filename[0]}");


            Workings.Methods.SoftWare("src\\tools\\ffmpeg.exe", $"-activation_bytes {bytes} -i {aax} -metadata:g encoding_tool=\"iAmDeaf 1.0\" -metadata title=\"{title}\" -metadata comment=\"{comment}\" -c copy {file}.m4b\" -y", true);

            
            Workings.Methods.SoftWare("src\\tools\\ffmpeg.exe", $"-i {aax} -map 0:v -map -0:V -c copy {file}.jpg\" -y", true);
            string nfo = Workings.Methods.nfo(aax, $"{file}.m4b\"");
            Workings.Methods.Alert("Generating nfo");
            File.WriteAllText(($"{file}.nfo\"").Replace("\"", ""), nfo, Encoding.UTF8);

            return 0;

        }
    }
}
