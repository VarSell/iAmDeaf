using AAXClean;
using AAXClean.Codecs;
using System;
using System.Text;
using System.Diagnostics;
using static Other;
using System.IO;
using System.Threading;

namespace Files
{
    internal class Create
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;
        public static string Nfo(string aax, string file, bool split = false)
        {
            string[] nfoPart = new string[15];
            try
            {
                aax = string.Concat("\"", aax, "\"");
                file = string.Concat("\"", file, "\"");

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
                nfoPart[10] = SoftWare(mi, $"{aax} --Inform=Audio;%Format%", false);            //audio format
                nfoPart[11] = SoftWare(mi, $"{aax} --Inform=Audio;%BitRate%", false);           //source bitrate
                try
                {
                    nfoPart[11] = (Int32.Parse(nfoPart[11]) / 1024).ToString();
                }
                catch
                {
                    nfoPart[11] = "NULL";
                    Alert.Error("Failed Getting Source BitRate");
                }
                if ((Path.GetExtension(file.Replace("\"", "")) == ".m4b"))
                {
                    nfoPart[12] = SoftWare(mi, $"{file} --Inform=General;%CodecID%", false); //encoded codecID
                }
                else
                {
                    string mp3enc = string.Empty;
                    switch (split)
                    {
                        case true:
                            mp3enc = "LAME 3.100";
                            break;
                        case false:
                            mp3enc = "Lavf59.16.100";
                            break;
                    }
                    nfoPart[12] = $"{mp3enc} MP3";
                }
                nfoPart[13] = (TagLib.File.Create(file.Replace("\"", ""))).Properties.AudioBitrate.ToString();
                nfoPart[14] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false); //comment (Track_More)
            }
            catch (Exception ex)
            {
                Alert.Error($"NFO Failed: {ex.Message}");
            }

            string nfo = @$"General Information
===================
 Title:                  {nfoPart[0]}
 Author:                 {nfoPart[1]}
 Narrator:               {nfoPart[2]}
 AudioBook Copyright:    {nfoPart[3].Replace("&#169;", string.Empty).Replace(";", " ")}
 Genre:                  {nfoPart[4]}
 Publisher:              {nfoPart[5]}
 Published:              {nfoPart[6]}
 Duration:               {nfoPart[7]}
 Chapters:               {nfoPart[8]}

Media Information
=================
 Source Format:          Audible {nfoPart[9].ToUpper()} ({nfoPart[10]})
 Source Bitrate:         {nfoPart[11]} kbps

 Lossless Encode:        {(Path.GetExtension(file.Replace("\"", "")) == ".m4b")}
 Encoded Codec:          {nfoPart[12]}
 Encoded Bitrate:        {nfoPart[13]} kbps

 Ripper:                 {Workings.iAmDeaf.mark} {Workings.iAmDeaf.version}

Publisher's Summary
===================
{nfoPart[14]}
";
            return nfo;
        }

        public static void Cuesheet(string aax, string file, string extention)
        {
            string format = "MP4";
            if (extention == "mp3")
            {
                format = "MP3";
            }
            string PID = Process.GetCurrentProcess().Id.ToString();

            SoftWare($@"{root}src\tools\ffmpeg.exe", $" -i \"{aax}\" -c copy -an {root}src\\data\\dump\\{PID}.mkv -y", true);
            SoftWare($@"{root}src\tools\mkvextract.exe", $" {root}src\\data\\dump\\{PID}.mkv chapters -s {root}src\\data\\dump\\{PID}.txt", true);

            string cuegenParams = $@"{root}src\tools\cuegen.vbs {root}src\\data\\dump\\{PID}.txt";
            var cueGen = Process.Start(@"cmd", @"/c " + cuegenParams);

            cueGen.WaitForExit();
            cueGen.Close();
            cueGen.Dispose();

            string[] cue = File.ReadAllLines($"{root}src\\data\\dump\\{PID}.cue");
            cue[0] = $"FILE \"{Path.GetFileName($"{file}.{extention}")}\" {format.ToUpper()}";
            File.WriteAllLines($"{file}.cue", cue);

            if (!File.Exists($"{file}.cue"))
            {
                Alert.Error("Cuesheet Failed");
            }
            else
            {
                Alert.Success("Cuesheet Created");
            }

            File.Delete($@"{root}src\data\dump\{PID}.cue");
            File.Delete($@"{root}src\data\dump\{PID}.txt");
            File.Delete($@"{root}src\data\dump\{PID}.mkv");
        }
    }
    public class Get
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;

        public static string[] AaxInformation(string aax)
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
    }
}