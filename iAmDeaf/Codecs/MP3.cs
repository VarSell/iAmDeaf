using System.Diagnostics;
using AAXClean;
using AAXClean.Codecs;
using iAmDeaf.Interfaces;
using CsAtomReader;
using NAudio.MediaFoundation;
using NAudio.Wave;
using NAudio.Lame;
using NAudio;
using static Other;


namespace iAmDeaf.Codecs
{
    internal class MP3 : IAudiobook
    {
        internal string sourceFile { get; set; }
        internal string outFile { get; set; }
        internal AAXClean.AaxFile encryptedFile { get; set; }
        internal string secret { get; set; }
        public bool Open(string file)
        {
            try
            {
                this.sourceFile = file;
                this.outFile = Path.GetDirectoryName(sourceFile);

                using (FileStream stream = new FileStream(sourceFile, FileMode.Open))
                {
                    string aaxTitle = new AtomReader(stream)
                    .GetMetaAtomValue(AtomReader.TitleTypeName)
                    .Replace(":", " -").Replace("?", "");

                    this.outFile = Path.Combine(outFile, string.Concat(aaxTitle, ".mp3"));
                }

                this.encryptedFile = new AAXClean.AaxFile(File.OpenRead(sourceFile));
                if (Path.GetExtension(this.sourceFile) == ".aax")
                {
                    this.secret = Audible.Secret.GetBytesFromFile(sourceFile);
                    this.encryptedFile.SetDecryptionKey(this.secret);
                }
                return true;
            }
            catch (Exception e)
            {
                Alert.Error(e.ToString());
                return false;
            }
        }
        public bool SetPathAndFileName(string file)
        {
            try
            {
                if (string.IsNullOrEmpty(file))
                {
                    Alert.Error("Path cannot be empty.");
                    return false;
                }

                this.outFile = file;
                return true;
            }
            catch (Exception e)
            {
                Alert.Error(e.ToString());
                return false;
            }
        }
        public void SetDecryptionKey(string license_key, string license_iv)
        {
            this.encryptedFile.SetDecryptionKey(license_key, license_iv);
        }
        public bool Encode(bool split = false)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                if (split)
                {
                    string splitPath = Path.Combine(Path.GetDirectoryName(this.outFile), Path.GetFileNameWithoutExtension(this.outFile));
                    if (!Directory.Exists(splitPath))
                    {
                        Directory.CreateDirectory(splitPath);
                    }

                    sw.Start();
                    var conversionResult = this.encryptedFile.ConvertToMultiMp3(this.encryptedFile.GetChapterInfo(), NewSplit);

                    void NewSplit(NewSplitCallback newSplitCallback)
                    {
                        string dir = splitPath;
                        string fileName = newSplitCallback.Chapter.Title.Replace(":", "") + ".mp3";
                        newSplitCallback.OutputFile = File.OpenWrite(Path.Combine(dir, fileName));
                    }
                    sw.Stop();
                }
                else
                {
                    string PID = Process.GetCurrentProcess().Id.ToString();
                    string root = AppDomain.CurrentDomain.BaseDirectory;
                    try
                    {
                        sw.Start();
                        string _f = Path.Combine(root, "src\\data\\dump", $"{PID}.mp4");
                        this.encryptedFile.ConvertToMp4a(File.Open($"{_f}", FileMode.OpenOrCreate, FileAccess.ReadWrite));
                        this.encryptedFile.Close();

                        TagLib.File mp4 = TagLib.File.Create(_f);
                        int br = Int32.Parse(string.Concat(mp4.Properties.AudioBitrate.ToString(), "000"));

                        string nrt = SoftWare($@"{root}\\src\tools\mediainfo.exe", $"\"{_f}\" --Inform=General;%nrt%", false);
                        string comment = SoftWare($@"{root}\\src\tools\mediainfo.exe", $"\"{_f}\" --Inform=General;%Track_More%", false);

                        Alert.Notify($"Lavf59.16.100 - {br.ToString()[..3]}_CBR");

                        LoadLameDLL();
                        MediaFoundationApi.Startup();

                        var aacFilePath = $@"{root}\\src\data\dump\{PID}.mp3";
                        Monitor(aacFilePath, _f);
                        using (var reader = new MediaFoundationReader(_f))
                        {
                            MediaFoundationEncoder.EncodeToMp3(reader, aacFilePath, br);
                        }
                        Console.WriteLine();
                        Alert.Notify("Tagging File");

                        SoftWare($@"{root}\src\tools\ffmpeg.exe", $"-i \"{_f}\" -i  \"{root}\\src\\data\\dump\\{PID}.mp3\" -map 1 -metadata Narrator=\"{nrt}\" -metadata Comment=\"{comment.Replace("\"", string.Empty)}\" -c copy \"{this.outFile}.mp3\" -y", true);
                        SoftWare($@"{root}\src\tools\ffmpeg.exe", $"-i \"{_f}\" -map 0:v -map -0:V -c copy \"{root}\\src\\data\\dump\\{PID}.jpg\" -y", true);
                        sw.Stop();
                    }
                    catch (Exception ex)
                    {
                        Alert.Error(ex.Message);
                        return false;
                    }
                    if (!(EmbedCoverArt(string.Concat(this.outFile, ".mp3"), $@"{root}src\data\dump\{PID}.jpg")))
                    {
                        Alert.Notify("Unable to set cover art");
                    }

                    File.Delete($@"{root}\\src\\data\\dump\\{PID}.mp4");
                    File.Delete($@"{root}\\src\\data\\dump\\{PID}.mp3");
                    File.Delete($@"{root}\\src\\data\\dump\\{PID}.jpg");
                }

                Alert.Notify(String.Format("Decrypted in {0}ms", sw.ElapsedMilliseconds.ToString()));
                return true;
            }
            catch (Exception e)
            {
                Alert.Error(e.ToString());
                return false;
            }
        }
        public bool Close()
        {
            try
            {
                this.encryptedFile.Close();
                return true;
            }
            catch (Exception e)
            {
                Alert.Error(e.ToString());
                return false;
            }
        }
        internal static bool EmbedCoverArt(string audioFilePath, string coverFilePath)
        {
            if (!(File.Exists(audioFilePath) || File.Exists(coverFilePath)))
            {
                Alert.Error("Audio and/or Cover file(s) do not exist.");
                return false;
            }

            try
            {
                // Формируем картинку в кадр Id3v2
                TagLib.Id3v2.Tag.DefaultVersion = 3;
                TagLib.Id3v2.Tag.ForceDefaultVersion = true;

                TagLib.File TagLibFile = TagLib.File.Create(audioFilePath);
                TagLib.Picture picture = new TagLib.Picture(coverFilePath);
                TagLib.Id3v2.AttachmentFrame albumCoverPictFrame = new TagLib.Id3v2.AttachmentFrame(picture);
                albumCoverPictFrame.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
                albumCoverPictFrame.Type = TagLib.PictureType.FrontCover;
                TagLib.IPicture[] pictFrames = new TagLib.IPicture[1];
                pictFrames[0] = (TagLib.IPicture)albumCoverPictFrame;
                TagLibFile.Tag.Pictures = pictFrames;
                TagLibFile.Save();

                return true;
            }
            catch (Exception ex)
            {
                Alert.Error($"Unable to set cover art: {ex.Message}");
                return false;
            }
        }
        internal static void LoadLameDLL()
        {
            LameDLL.LoadNativeDLL(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\data"));
        }
        internal static NAudio.Lame.LAMEPreset Preset(string file)
        {
            int bitrate = (Int32.Parse(SoftWare(@"src\tools\mediainfo.exe", $"\"{file}\" --Inform=Audio;%BitRate%", false)) / 1024);

            switch (bitrate)
            {
                case >= 124:
                    Alert.Notify($"LAME 3.100 - V1"); return LAMEPreset.V1;
                case >= 95:
                    Alert.Notify($"LAME 3.100 - ABR_96"); return LAMEPreset.ABR_96;
                case >= 60:
                    Alert.Notify($"LAME 3.100 - ABR_64"); return LAMEPreset.ABR_64;
                case >= 31:
                    Alert.Notify($"LAME 3.100 - ABR_32"); return LAMEPreset.ABR_32;
                default:
                    Alert.Notify($"LAME 3.100 - ABR_32"); return LAMEPreset.ABR_32;
            }
        }
        private static async void Monitor(string file, string oFile)
        {
            await Task.Run(() =>
            {
                decimal buffer;
                decimal oSize = Decimal.Round((decimal)new FileInfo(oFile).Length / 1048576, 2);
                while (!File.Exists(file))
                {
                    Thread.Sleep(700);
                }
                while (true)
                {
                    buffer = (decimal)new FileInfo(file).Length;
                    Console.Write($"  S: {oSize} O: {Decimal.Round(buffer / 1048576, 2)} MiB");
                    Console.Write("\r");
                    Thread.Sleep(270);
                    if (buffer == (new System.IO.FileInfo(file).Length))
                    {
                        Alert.Success($"File size of {Decimal.Round(buffer / 1048576, 2)} MiB");
                        break;
                    }
                }
            });
        }
    }
}
