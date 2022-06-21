using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using Files;
using static Other;
using iAmDeaf.Audible;
using CsAtomReader;
using iAmDeaf.Interfaces;
using iAmDeaf.Codecs;
using iAmDeaf.Plus;

using Workings;



namespace Workings
{
    public class iAmDeaf
    {
        public const string mark = "iAmDeaf";
        public const string version = "2.0.3";
    }
}

namespace Main
{
    internal class Program
    {
        public static string root = AppDomain.CurrentDomain.BaseDirectory;
        public static iAmDeaf.Config.Settings? settings;
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
                        Catalogue.Download(args[1]);
                    }
                    catch (Exception ex)
                    {
                        Alert.Error(ex.Message);
                    }
                    return 0;
                }

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

            if (Path.GetExtension(aax) == ".aaxc")
            {
                //Local.GetPaths(aax);
                //return 0;
            }

            Console.CursorVisible = false;

            string[] filename;
            string title, file = string.Empty;
            string codec = "m4b";
            bool cueEnabled = true;
            bool nfoEnabled = true;
            bool coverEnabled = true;
            bool split = false;
            string hostDir = Path.GetDirectoryName(aax);

            Alert.Notify("Parsing File");
            
            settings = JsonConvert.DeserializeObject<iAmDeaf.Config.Settings>(File.ReadAllText($"{root}\\src\\config.json"));
            string structure = $"{settings.Title[0].T1} {settings.Title[0].T2} {settings.Title[0].T3} {settings.Title[0].T4} {settings.Title[0].T5}";
            structure = Regex.Replace(structure.Replace("null", null), @"\s+", " ").Trim();
            structure = structure.Replace(" ", " - ");

            
            string aaxTitle = string.Empty;
            using (FileStream stream = new FileStream(aax, FileMode.Open))
            {
                aaxTitle = new AtomReader(stream)
                .GetMetaAtomValue(AtomReader.TitleTypeName).Replace(":", " -");
            }

            try
            {
                if (settings.DEFAULT)
                {
                    string aaxAuthor = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Performer%", false).Trim();                                      //Author       
                          
                    string aaxYear = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%rldt%", false).Trim();                                             //Year        
                    aaxYear = DateTime.ParseExact(aaxYear, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("yyyy");          
                    string aaxNarrator = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%nrt%", false).Trim();                                          //Narrator         
                    string aaxBitrate = (Int32.Parse(SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=Audio;%BitRate%", false).Trim()) / 1000).ToString() + "K";  //Bitrate            //Bitrate

                    /*string[] onlineDetails = Scraper.Scrape(SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{aax}\" --Inform=General;%Album%", false));

                    string oAuthor = onlineDetails[4];
                    string oTitle = onlineDetails[2];
                    file = string.Concat(oAuthor.Trim(), " - ", oTitle.Trim()).Replace("?", "");*/

                    file = structure.Replace("Author", aaxAuthor)
                        .Replace("Title", aaxTitle)
                        .Replace("Year", aaxYear)
                        .Replace("Narrator", aaxNarrator)
                        .Replace("Bitrate", aaxBitrate)
                        .TrimEnd('.');

                    file = GetSafeFilename(file);

                    Alert.Success(file);

                    codec = settings.Output[0].Codec;
                    split = settings.Output[0].Split;
                    cueEnabled = settings.Files[0].Cue;
                    nfoEnabled = settings.Files[0].NFO;
                    coverEnabled = settings.Files[0].Cover;

                    Directory.CreateDirectory($"{hostDir}\\{file}");
                    title = file;
                    file = $"{hostDir}\\{file}\\{file.Trim()}";
                }
                else
                {
                    file = GetPreferredFilename(aax);

                    Alert.Success(file);

                    string _f = file;
                    file = Path.Combine(hostDir, file, file);
                    Directory.CreateDirectory(Path.Combine(hostDir, _f));
                }
            }
            catch
            {
                Alert.Error("Bad config");
                title = aaxTitle;
                aaxTitle = GetSafeFilename(aaxTitle.Trim().Replace(":", " -").TrimEnd('.'));
                file = GetSafeFilename(aaxTitle);

                Alert.Success(file);

                file = Path.Combine(hostDir, aaxTitle, file);
                Directory.CreateDirectory(Path.Combine(hostDir, aaxTitle));
            }

            IAudiobook audio;

            switch (codec.ToLower())
            {
                case "m4b":
                    audio = Select.M4b();
                    break;
                case "mp3":
                    audio = Select.Mp3();
                    break;
                default: audio = Select.M4b();
                    break;
            }
            audio.Open(aax);
            audio.SetPathAndFileName(file);
            if (Path.GetExtension(aax) == ".aaxc")
            {
                try
                {
                    string voucher = Path.Combine(Path.GetDirectoryName(aax), string.Concat(Path.GetFileNameWithoutExtension(aax), ".voucher"));
                    if (!File.Exists(voucher))
                    {
                        Alert.Error("Voucher not found.");
                        return 0;
                    }
                    else
                    {
                        Alert.Notify(Path.GetFileName(voucher));
                    }
                    Voucher.Rootobject license = JsonConvert.DeserializeObject<Voucher.Rootobject>(File.ReadAllText(voucher));
                    audio.SetDecryptionKey(license.content_license.license_response.key, license.content_license.license_response.iv);
                }
                catch (Exception e)
                {
                    Alert.Error($"Unable to parse voucher: {e.Message}");
                }
            }

            Thread cueThr = new Thread(() =>
            {
                Create.Cuesheet(aax, file);
            });
            Thread audioThr = new Thread(() =>
            {
                audio.Encode(split);
                audio.Close();
                if (nfoEnabled)
                {
                    string nfo;
                    if (!split)
                    {
                        Alert.Notify("Generating NFO");
                        nfo = Create.Nfo(aax, $"{file}.{codec}");
                    }
                    else
                    {
                        string[] extensions = { string.Concat('.', codec) };
                        var files = Directory.GetFiles(Path.GetDirectoryName(file), ".")
                            .Where(f => Array.Exists(extensions, e => f.EndsWith(e))).ToArray();
                        Alert.Notify("Generating NFO");
                        nfo = Create.Nfo(aax, files[0], split);
                    }
                    File.WriteAllText($"{file}.nfo", nfo, Encoding.UTF8);
                }
            });

            Stopwatch sw = new Stopwatch();
            sw.Start();

            audioThr.Start();

            if (cueEnabled)
            {
                Alert.Notify("Generating cuesheet");
                cueThr.Start();
                if (coverEnabled)
                {
                    Alert.Notify("Extracting JPG");
                    SoftWare($"{root}src\\tools\\ffmpeg.exe", $"-i \"{aax}\" -map 0:v -map -0:V -c copy \"{file}.jpg\" -y", true);
                }
            }

            if (cueThr.IsAlive)
            {
                cueThr.Join();
            }

            audioThr.Join();

            sw.Stop();
            Alert.Notify($"Execution: {(sw.ElapsedMilliseconds / 1000).ToString()}s");

            Console.CursorVisible = true;
            return 0;
        }
    }
}