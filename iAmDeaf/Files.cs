using System;
using System.Text;
using System.Diagnostics;
using AAXClean;
using static Other;


namespace Files
{
    internal class Create
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;
        public static string nfo(string aax, string file)
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
                nfoPart[10] = SoftWare(mi, $"{file} --Inform=Audio;%Format%", false);            //audio format
                nfoPart[11] = SoftWare(mi, $"{file} --Inform=Audio;%BitRate%", false);    //source bitrate
                try
                {
                    nfoPart[11] = (Int32.Parse(nfoPart[11]) / 1000).ToString();
                }
                catch
                {
                    nfoPart[11] = "NULL";
                    Alert.Error("Failed Getting Source BitRate");
                }
                nfoPart[12] = SoftWare(mi, $"{file} --Inform=General;%CodecID%", false); //encoded codecID
                nfoPart[13] = SoftWare(mi, $"{file} --Inform=Audio;%BitRate%", false);   //encoded bitrate
                try
                {
                    nfoPart[13] = (Int32.Parse(nfoPart[13]) / 1000).ToString();
                }
                catch
                {
                    nfoPart[13] = "NULL";
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
 Title:                  {nfoPart[0]}
 Author:                 {nfoPart[1]}
 Narrator:               {nfoPart[2]}
 AudioBook Copyright:    {nfoPart[3]}
 Genre:                  {nfoPart[4]}
 Publisher:              {nfoPart[5]}
 Published:              {nfoPart[6]}
 Duration:               {nfoPart[7]}
 Chapters:               {nfoPart[8]}

Media Information
=================
 Source Format:          Audible {nfoPart[9].ToUpper()} ({nfoPart[10]})
 Source Bitrate:         {nfoPart[11]} kbps

 Encoded Codec:          {nfoPart[12]}
 Encoded Bitrate:        {nfoPart[13]} kbps

Ripper:                  {Workings.iAmDeaf.mark} {Workings.iAmDeaf.version}

Publisher's Summary
===================
{nfoPart[14]}
";
            return nfo;
        }

        public static void Cue(string aax, string file)
        {
            string PID = Process.GetCurrentProcess().Id.ToString();

            SoftWare($@"{root}src\tools\ffmpeg.exe", $" -i \"{aax}\" -c copy -an {root}src\\data\\dump\\{PID}.mkv -y", true);

            SoftWare($@"{root}src\tools\mkvextract.exe", $" {root}src\\data\\dump\\{PID}.mkv chapters -s {root}src\\data\\dump\\{PID}.txt", true);

            string cuegen = $@"{root}src\tools\cuegen.vbs {root}src\\data\\dump\\{PID}.txt";

            var CUEGEN = Process.Start(@"cmd", @"/c " + cuegen);

            CUEGEN.WaitForExit();
            CUEGEN.Close();
            CUEGEN.Dispose();

            string[] cue = File.ReadAllLines($"{root}src\\data\\dump\\{PID}.cue");

            cue[0] = $"FILE \"{Path.GetFileName($"{file}.m4b")}\" MP4";

            
            File.WriteAllLines($"{file}.cue", cue);

            if (!File.Exists($"{file}.cue"))
            {
                Alert.Error("Cue Failed");
            }
            else
            {
                Alert.Success("Cue Created");
            }

            File.Delete($@"{root}src\data\dump\{PID}.cue");
            File.Delete($@"{root}src\data\dump\{PID}.txt");
            File.Delete($@"{root}src\data\dump\{PID}.mkv");
        }

        public static void AudioBook(string bytes, string aax, string file, string ext = "m4b", bool split = false)
        {
            if (!(ext == "m4b" || ext == "mp3"))
            {
                Alert.Notify($"Invalid codec {ext}. Defaulting to M4B");
                ext = "m4b";
            }

            var aaxFile = new AaxFile(File.OpenRead(aax));
            aaxFile.SetDecryptionKey(bytes);

            if (ext == "m4b")
            {
                if (!split)
                {
                    try
                    {
                        aaxFile.ConvertToMp4a(File.Open($"{file}.m4b", FileMode.OpenOrCreate, FileAccess.ReadWrite));

                        if (File.Exists($"{file}.m4b"))
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
                else
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\data\dump\host"), Path.GetDirectoryName(file));

                        aaxFile.ConvertToMultiMp4a(aaxFile.GetChapterInfo(), NewSplit);

                        static void NewSplit(NewSplitCallback newSplitCallback)
                        {
                            string dir = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\data\dump\host"));

                            string fileName = newSplitCallback.Chapter.Title.Replace(":", "") + ".m4b";

                            newSplitCallback.OutputFile = File.OpenWrite(Path.Combine(dir, fileName));
                        }
                    }
                    catch (Exception ex)
                    {
                        Alert.Error(ex.Message);
                    }
                }
            }
            /*if (ext == "mp3") // Fuck, the fixed aaxclean does not support mp3 conversion
            {
                try
                {
                    aaxFile.ConvertToMp3(File.Open($"{file}.mp3", FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    if (File.Exists($"{file}.m4b"))
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
            }*/
        }
    }

    class Get
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;
        public static string ActivationBytes(string aax)
        {
            string currentSum = $"{root}\\src\\data\\KeyHistory\\CurrentSum";
            if (!File.Exists(currentSum))
            {
                File.Create(currentSum).Dispose();
            }
            string cacheBytes = $"{root}src\\data\\KeyHistory\\CurrentBytes";
            string checksum = SoftWare($@"{root}src\\tools\\ffprobe.exe", $"\"{aax}\"", true);
            File.WriteAllText(currentSum, checksum.Replace(" ", ""));
            string[] line = File.ReadAllLines(currentSum);
            checksum = (line[11].Split("==").Last());
            File.WriteAllText(currentSum, checksum);

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
             * Just as a reminder, this is where current dir is changed, as rcrack doesnt like to be launched when it's not in its root dir without its files
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