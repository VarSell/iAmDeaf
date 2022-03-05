using AAXClean;
using Newtonsoft.Json;
using static Other;
using Files;
using System.Diagnostics;

namespace Plus
{
    internal class Catagolue
    {
        public static void Download(string ASIN)
        {
            Rootobject1 Settings = JsonConvert.DeserializeObject<Rootobject1>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\config.json")));

            bool nfo = Settings.AAXC[0].NFO;
            bool Cue = Settings.AAXC[0].Cue;
            bool Cover = Settings.AAXC[0].Cover;
            bool Split = Settings.AAXC[0].Split;
            bool Backup = Settings.AAXC[0].Backup;
            string param = string.Empty;
            if (Cover) { param = "--cover --cover-size 1215"; }

            Alert.Notify("Downloading");
            SoftWare(@"src\tools\audible.exe", $@"download -a {ASIN} {param} -o {AppDomain.CurrentDomain.BaseDirectory}src\data\dump --aaxc", false);

            Alert.Notify("Parsing Voucher");
            var Keys = ParseVoucher();

            Alert.Notify("Creating Audiobook");
            if (!AAXCDecrypt(Keys[0], Keys[1], nfo, Cue, Cover, Split))
            {
                Alert.Error("AAXC Decryption Failed");
            }
            else
            {
                Cleanup(Backup);
                Alert.Success("Audiobook Created");
            }
        }

        public static bool AAXCDecrypt(string key, string iv, bool nfo, bool Cue, bool Cover, bool Split)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (!Directory.Exists(Path.Combine(root, "Audiobooks")))
            {
                Directory.CreateDirectory(Path.Combine(root, "Audiobooks"));
            }

            var file = Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.aaxc")[0];


            string filename = Other.SoftWare(@"src\tools\mediainfo.exe", $"\"{file}\" --Inform=General;%Album%", false);
            filename = filename.Trim().Replace(":", " -");
            string t1, t2, t3 = string.Empty;
            try
            {
                t1 = filename.Split(",").Last().Trim();
                t2 = t1.Replace(t1, null).Replace(",", null).Split(":").Last().Trim();
                t3 = filename.Replace(t1, null).Replace(t2, null).Replace(":", null).Replace(",", null).Trim();

                filename = ($"{t2} [{t1}] {t3}");
            }
            catch
            {
                //
            }
            Alert.Success(filename);

            var aaxcFile = new AaxFile(File.OpenRead(file));
            aaxcFile.SetDecryptionKey(key, iv);
            try
            {
                string PID = Process.GetCurrentProcess().Id.ToString();
                var hostDIR = Path.Combine(root, "Audiobooks", filename);
                if (!Directory.Exists(Path.Combine(root, "Audiobooks", filename)))
                {
                    Directory.CreateDirectory(Path.Combine(root, "Audiobooks", filename));
                }
                if (!Split)
                {
                    aaxcFile.ConvertToMp4a(File.Open($@"{hostDIR}\{filename}.m4b", FileMode.OpenOrCreate, FileAccess.ReadWrite));
                }
                else
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"src\data\dump\host"), Path.Combine(root, "Audiobooks", filename));
                    aaxcFile.ConvertToMultiMp4a(aaxcFile.GetChapterInfo(), NewSplit);

                    static void NewSplit(NewSplitCallback newSplitCallback)
                    {
                        string dir = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"src\data\dump\host"));

                        string fileName = newSplitCallback.Chapter.Title.Replace(":", "") + ".m4b";

                        newSplitCallback.OutputFile = File.OpenWrite(Path.Combine(dir, fileName));
                    }
                }
                aaxcFile.Close();
                

                if (Cover)
                {
                    Alert.Notify("Moving Cover");
                    File.Move((Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}src\data\dump\", "*.jpg")[0]), ($@"{hostDIR}\\{filename}.jpg"));
                }

                if (nfo)
                {
                    Alert.Notify("Generating nfo");
                    string nfnm = filename;
                    if (Split) { filename = Path.GetFileNameWithoutExtension(Directory.GetFiles(hostDIR, "*.m4b")[0]); }
                    File.WriteAllText($@"{hostDIR}\{nfnm}.nfo", Create.nfo(file, $@"{hostDIR}\{filename}.m4b", Split));
                }

                if (Cue && !Split)
                {
                    Alert.Notify("Generating Cue");
                    Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                    SoftWare("src\\tools\\ffmpeg.exe", $"-i \"{file}\" -c copy -an src\\data\\dump\\{PID}.mkv -y", true);
                    SoftWare($@"src\tools\mkvextract.exe", $"src\\data\\dump\\{PID}.mkv chapters -s src\\data\\dump\\{PID}.txt", true);
                    string cuegen = $@"src\tools\cuegen.vbs src\\data\\dump\\{PID}.txt";

                    var CUEGEN = Process.Start(@"cmd", @"/c " + cuegen);
                    CUEGEN.WaitForExit();
                    CUEGEN.Close();
                    CUEGEN.Dispose();

                    string[] cue = File.ReadAllLines($"src\\data\\dump\\{PID}.cue");
                    cue[0] = $"FILE \"{filename}.m4b\" MP4";
                    File.WriteAllLines($@"{hostDIR}\{filename}.cue", cue);

                    File.Delete($@"src\data\dump\{PID}.cue");
                    File.Delete($@"src\data\dump\{PID}.txt");
                    File.Delete($@"src\data\dump\{PID}.mkv");
                }

                return true;
            }
            catch (Exception ex)
            {
                aaxcFile.Close();
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

                Rootobject Settings = JsonConvert.DeserializeObject<Rootobject>(File.ReadAllText(Voucher));

                return new string[] { Settings.content_license.license_response.key, Settings.content_license.license_response.iv };
            }
            catch (Exception ex)
            {
                Alert.Error(ex.Message);
                return null;
            }
        }
    }




    public class Rootobject
    {
        public Content_License content_license { get; set; }
        public string[] response_groups { get; set; }
    }

    public class Content_License
    {
        public string acr { get; set; }
        public string asin { get; set; }
        public Content_Metadata content_metadata { get; set; }
        public string drm_type { get; set; }
        public string license_id { get; set; }
        public License_Response license_response { get; set; }
        public string message { get; set; }
        public string request_id { get; set; }
        public bool requires_ad_supported_playback { get; set; }
        public string status_code { get; set; }
        public string voucher_id { get; set; }
    }

    public class Content_Metadata
    {
        public Chapter_Info chapter_info { get; set; }
        public Content_Reference content_reference { get; set; }
        public Content_Url content_url { get; set; }
        public Last_Position_Heard last_position_heard { get; set; }
    }

    public class Chapter_Info
    {
        public int brandIntroDurationMs { get; set; }
        public int brandOutroDurationMs { get; set; }
        public Chapter[] chapters { get; set; }
        public bool is_accurate { get; set; }
        public int runtime_length_ms { get; set; }
        public int runtime_length_sec { get; set; }
    }

    public class Chapter
    {
        public int length_ms { get; set; }
        public int start_offset_ms { get; set; }
        public int start_offset_sec { get; set; }
        public string title { get; set; }
    }

    public class Content_Reference
    {
        public string acr { get; set; }
        public string asin { get; set; }
        public string content_format { get; set; }
        public int content_size_in_bytes { get; set; }
        public string file_version { get; set; }
        public string marketplace { get; set; }
        public string sku { get; set; }
        public string tempo { get; set; }
        public string version { get; set; }
    }

    public class Content_Url
    {
        public string offline_url { get; set; }
    }

    public class Last_Position_Heard
    {
        public string status { get; set; }
    }

    public class License_Response
    {
        public string key { get; set; }
        public string iv { get; set; }
        public Rule[] rules { get; set; }
    }

    public class Rule
    {
        public Parameter[] parameters { get; set; }
        public string name { get; set; }
    }

    public class Parameter
    {
        public DateTime expireDate { get; set; }
        public string type { get; set; }
    }


    public class Rootobject1
    {
        public AAXC[] AAXC { get; set; }
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





