using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using System.Diagnostics;
using static Other;
using iAmDeaf.Audible;
using CsAtomReader;
using iAmDeaf.Interfaces;
using iAmDeaf.Codecs;
using iAmDeaf.Plus;
using iAmDeaf.Other;
namespace Main
{
    using Mp4Chapters;
    using Newtonsoft.Json.Linq;

    internal class Program
    {
        internal const string MARK = "iAmDeaf";
        internal const string VERSION = "2.0.3";
        public static string root = AppDomain.CurrentDomain.BaseDirectory;

        internal static string Title
        { get; set; }
        internal static string DestinationFile
        { get; set; }
        internal static string Aax
        { get; set; }
        internal static Boolean CueEnabled
        { get; set; }
        internal static Boolean NfoEnabled
        { get; set; }
        internal static Boolean CoverEnabled
        { get; set; }
        internal static Boolean SplitFile
        { get; set; }
        internal static string HostDirectory
        { get; set; }
        internal static string Root = AppDomain.CurrentDomain.BaseDirectory;

        static int Main(string[] args)
        {
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
                        Record.Log(ex, new StackTrace(true));
                    }
                    return 0;
                }

                foreach (Object obj in args)
                {
                    aax = obj.ToString();
                }

                if (!File.Exists(Aax))
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

            Console.CursorVisible = false;

            

            Alert.Notify("Parsing File");
            dynamic settings = JObject.Parse(File.ReadAllText(Path.Combine(Root, "src\\configuration.json")));
            string preferredFilename = String.Concat((string)settings["Title"][0]["T1"], " ", (string)settings.["Title"][0]["T2"], " ", (string)settings["Title"][0]["T3"], " ", (string)settings["Title"][0]["T4"], " ", (string)settings["Title"][0]["T5"]);
            
            preferredFilename = Regex.Replace(preferredFilename.Replace("null", null), @"\s+", " ").Trim().Replace (" ", " - ");

            
            using (FileStream stream = new FileStream(Aax, FileMode.Open))
            {
                Title = new AtomReader(stream)
                .GetMetaAtomValue(AtomReader.TitleTypeName).Replace(":", " -");
            }

            try
            {
                if (Convert.ToBoolean(settings["DEFAULT"]))
                {
                    string aaxAuthor = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{Aax}\" --Inform=General;%Performer%", false).Trim();                          
                    string aaxYear = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{Aax}\" --Inform=General;%rldt%", false).Trim(); 
                    aaxYear = DateTime.ParseExact(aaxYear, "dd-MMM-yyyy", CultureInfo.InvariantCulture).ToString("yyyy");
                    string aaxNarrator = SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{Aax}\" --Inform=General;%nrt%", false).Trim();     
                    string aaxBitrate = (Int32.Parse(SoftWare($"{root}src\\tools\\mediainfo.exe", $"\"{Aax}\" --Inform=Audio;%BitRate%", false).Trim()) / 1000).ToString() + "K";

                    DestinationFile = preferredFilename.Replace("Author", aaxAuthor)
                        .Replace("Title", aaxTitle)
                        .Replace("Year", aaxYear)
                        .Replace("Narrator", aaxNarrator)
                        .Replace("Bitrate", aaxBitrate)
                        .TrimEnd('.');

                    DestinationFile = GetSafeFilename(DestinationFile);

                    Alert.Success(DestinationFile);

                    Codec = (string)settings["Output"][0]["Codec"];
                    SplitFile = (Boolean)settings["Output"][0]["Split"];
                    cueEnabled = settings.Files[0].Cue;
                    nfoEnabled = settings.Files[0].NFO;
                    coverEnabled = settings.Files[0].Cover;

                    Directory.CreateDirectory(Path.Combine(HostDirectory, DestinationFile));
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
            catch (Exception ex)
            {
                Alert.Error("Bad config");
                Record.Log(ex, new StackTrace(true));
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
                catch (Exception ex)
                {
                    Alert.Error($"Unable to parse voucher.");
                    Record.Log(ex, new StackTrace(true));
                }
            }

            Thread cueThr = new Thread(() =>
            {
                try
                {
                    iAmDeaf.Util.Create.CueSheet(Aax, DestinationFile, Codec);
                }
                catch (Exception ex)
                {
                    Alert.Error(ex.Message);
                }
            });
            Thread audioThr = new Thread(() =>
            {
                audio.Encode(SplitFile);
                audio.Close();
                if (NfoEnabled)
                {
                    string nfo;
                    if (!SplitFile)
                    {
                        Alert.Notify("Generating NFO");
                        nfo = iAmDeaf.Util.Create.Nfo(Aax, String.Concat(DestinationFile, Codec));
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
                    SoftWare($"\"{root}src\\tools\\ffmpeg.exe\"", $"-i \"{aax}\" -map 0:v -map -0:V -c copy \"{file}.jpg\" -y", true);
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