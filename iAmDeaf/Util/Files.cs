using AAXClean;
using AAXClean.Codecs;
using System;
using System.Text;
using System.Diagnostics;
using static Other;
using System.IO;
using Mp4Chapters;
using Main;

namespace iAmDeaf.Util
{
    internal static class Create
    {
        internal static string Root = AppDomain.CurrentDomain.BaseDirectory;
        public static string Nfo(string aax, string file, bool split = false)
        {
            string[] nfoPart = new string[15];
            try
            {
                aax = string.Concat("\"", aax, "\"");
                file = string.Concat("\"", file, "\"");

                string mi = $"{Root}\\src\\tools\\mediainfo.exe";
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
                    nfoPart[11] = (int.Parse(nfoPart[11]) / 1024).ToString();
                }
                catch
                {
                    nfoPart[11] = "NULL";
                    Alert.Error("Failed Getting Source BitRate");
                }
                if (Path.GetExtension(file.Replace("\"", "")) == ".m4b")
                {
                    nfoPart[12] = SoftWare(mi, $"{file} --Inform=General;%CodecID%", false);
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
                nfoPart[13] = TagLib.File.Create(file.Replace("\"", "")).Properties.AudioBitrate.ToString();
                nfoPart[14] = SoftWare(mi, $"{aax} --Inform=General;%Track_More%", false);
            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
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

 Lossless Encode:        {Path.GetExtension(file.Replace("\"", "")) == ".m4b"}
 Encoded Codec:          {nfoPart[12]}
 Encoded Bitrate:        {nfoPart[13]} kbps

 Ripper:                 {Program.MARK} {Program.VERSION}

Publisher's Summary
===================
{nfoPart[14]}
";
            return nfo;
        }
        public static void CueSheet(string aax, string file, string codec)
        {
            string format = "MP4";
            if (codec != "m4b")
            {
                format = "MP3";
            }

            try
            {
                string performer = SoftWare($@"{root}\src\tools\mediainfo.exe", $"{aax} --Inform=General;%Performer%", false);
                string date = SoftWare($@"{root}\src\tools\mediainfo.exe", $"{aax} --Inform=General;%rldt%", false);
                string title = SoftWare($@"{root}\src\tools\mediainfo.exe", $"{aax} --Inform=General;%Album%", false);

                date = DateTime.Parse(date).ToString("yyyy");

                List<string> cueSheet = new List<string>();
                cueSheet.Add("REM GENRE Audiobook");
                cueSheet.Add($"REM DATE {date}");
                cueSheet.Add($"PERFORMER \"{performer}\"");
                cueSheet.Add($"TITLE \"{title}\"");
                cueSheet.Add($"FILE \"{Path.GetFileNameWithoutExtension(file)}.{codec}\" {format}");

                using (var str = File.OpenRead(aax))
                {
                    int i = 1;
                    var extractor = new ChapterExtractor(new StreamWrapper(str));
                    extractor.Run();

                    foreach (var c in extractor.Chapters)
                    {
                        string pos = string.Empty;
                        if (i<10)
                        {
                            pos = new string(String.Concat('0', i));
                        }
                        else
                        {
                            pos = (i).ToString();
                        }
                        string time = (c.Time.ToString(@"dd\:hh\:mm\:ss\:fff"));
                        
                        string d = (time.Split(':')[0]);
                        string h = (time.Split(':')[1].Split(':')[0]);
                        string m = (time.Split(':')[2].Split(':')[0]);
                        string s = (time.Split(':')[3].Split(':')[0]);
                        string ms = (time.Split(':')[4].Split(':')[0]);

                        string cueFrames = ((int)(Int32.Parse(ms) * 0.075)).ToString();
                        if (cueFrames.Length == 1)
                            cueFrames = String.Concat('0', cueFrames);
                        
                        string cueMin = ((((Int32.Parse(d) * 24) + Int32.Parse(h)) * 60) + Int32.Parse(m)).ToString();

                        if (cueMin.ToString().Length == 1)
                            cueMin = String.Concat('0', cueMin);

                        cueSheet.Add($"  TRACK {pos} AUDIO");
                        cueSheet.Add($"    TITLE \"{c.Name}\"");
                        cueSheet.Add($"    INDEX 01 {cueMin}:{s}:{cueFrames}");

                        i++;
                    }
                }

                using (TextWriter tw = new StreamWriter(String.Concat(file, ".cue")))
                {
                    foreach (String ln in cueSheet)
                        tw.WriteLine(ln);
                }
            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
            }
        }
    }
    public class Get
    {
        public static string[] AaxInformation(string aax)
        {
            aax = string.Concat("\"", aax, "\"");
            string mi = Path.Combine(Root, "src\\tools\\mediainfo.exe");
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
                info[1] = string.Concat("0", info[1]);
            }

            return info;
        }
    }
}