using AAXClean;
using Newtonsoft.Json;
using static Other;
using Files;
using System.Diagnostics;
using iAmDeaf.Audible;

namespace iAmDeaf.Plus
{
    internal static class Catalogue
    {
        public static void Download(string ASIN)
        {
            Config.Settings settings = JsonConvert.DeserializeObject<Config.Settings>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\config.json")));

            bool nfo = settings.AAXC[0].NFO;
            string codec = settings.Output[0].Codec;
            bool cue = settings.AAXC[0].Cue;
            bool cover = settings.AAXC[0].Cover;
            bool split = settings.AAXC[0].Split;
            bool backup = settings.AAXC[0].Backup;
            string param = string.Empty;
            if (cover)
            {
                param = "--cover --cover-size 1215";
            }
            
            Alert.Notify("Downloading");
            SoftWare(@"src\tools\audible.exe", $@"download -a {ASIN} {param} -o {AppDomain.CurrentDomain.BaseDirectory}src\data\dump --aaxc", false);

            Alert.Notify("Parsing Voucher");
            var keys = ParseVoucher();

            Alert.Notify("Creating Audiobook");
            if (!AAXCDecrypt(keys[0], keys[1], nfo, cue, cover, split, codec))
            {
                Alert.Error("AAXC Decryption Failed");
            }
            else
            {
                Cleanup(backup);
                Alert.Success("Audiobook Created");
            }
        }

        public static bool AAXCDecrypt(string key, string iv, bool nfo, bool cue, bool cover, bool split, string codec)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (!Directory.Exists(Path.Combine(root, "Audiobooks")))
            {
                Directory.CreateDirectory(Path.Combine(root, "Audiobooks"));
            }

            var file = Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.aaxc")[0];
            string filename = GetPreferredFilename(file);

            Alert.Success(filename);

            try
            {
                string PID = Process.GetCurrentProcess().Id.ToString();
                var fileDir = Path.Combine(root, "Audiobooks", filename);
                if (!Directory.Exists(fileDir))
                {
                    Directory.CreateDirectory(fileDir);
                }

                Interfaces.IAudiobook audio;
                switch (codec.ToLower())
                {
                    case "m4b":
                        audio = Codecs.Select.M4b();
                        break;
                    case "mp3":
                        audio = Codecs.Select.Mp3();
                        break;
                    default:
                        audio = Codecs.Select.M4b();
                        break;
                }

                audio.Open(file);
                audio.SetDecryptionKey(key, iv);
                audio.SetPathAndFileName(Path.Combine(fileDir, filename));
                audio.Encode(split);
                audio.Close();

                if (cue)
                {
                    Create.Cuesheet(file, Path.Combine(fileDir, filename));
                }

                if (cover)
                {
                    Alert.Notify("Moving Cover");
                    File.Move(Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.jpg")[0], $@"{fileDir}\{filename}.jpg");
                }

                if (nfo)
                {
                    Alert.Notify("Generating nfo");
                    Create.Nfo(file, $"{Path.Combine(fileDir, filename)}.{codec}");
                    /*string nfnm = filename;
                    if (split) { filename = Path.GetFileNameWithoutExtension(Directory.GetFiles(fileDir, "*.m4b")[0]); }*/
                    File.WriteAllText($"{Path.Combine(fileDir, filename)}.nfo", Create.Nfo(file, $"{Path.Combine(fileDir, filename)}.{codec}", split), System.Text.Encoding.UTF8);
                }

                return true;
            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
                return false;
            }
        }

        public static void Cleanup(bool bak)
        {

            string aaxc = Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.aaxc")[0];
            string voucher = Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.voucher")[0];
            if (!bak)
            {
                Alert.Success("Wiping temp files");
                File.Delete(aaxc);
                File.Delete(voucher);
            }
            else
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), @"Audiobooks\bak");
                string bakdir = Path.Combine(dir, Path.GetFileNameWithoutExtension(aaxc));
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!Directory.Exists(bakdir))
                {
                    Directory.CreateDirectory(bakdir);
                }
                Alert.Success("Backing up files");
                File.Move(aaxc, Path.Combine(bakdir, Path.GetFileName(aaxc)));
                File.Move(voucher, Path.Combine(bakdir, Path.GetFileName(voucher)));
            }
        }


        public static string[] ParseVoucher()
        {
            try
            {
                var Voucher = Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.voucher")[0];

                Voucher.Rootobject Settings = JsonConvert.DeserializeObject<Voucher.Rootobject>(File.ReadAllText(Voucher));

                return new string[] { Settings.content_license.license_response.key, Settings.content_license.license_response.iv };
            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
                return null;
            }
        }
    }

}