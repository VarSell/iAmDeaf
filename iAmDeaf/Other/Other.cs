using System.Text;
using System.Diagnostics;
using iAmDeaf.Other;

internal class Other
{
    public static string SoftWare(string software, string arguments, bool std)
    {
        Process SoftWare = new Process();
        SoftWare.StartInfo.FileName = software;
        SoftWare.StartInfo.Arguments = arguments;

        if (std == true)
        {
            SoftWare.StartInfo.RedirectStandardError = true;
            SoftWare.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            SoftWare.Start();

            using (StreamReader reader = SoftWare.StandardError)
            {
                string result = reader.ReadToEnd();
                SoftWare.WaitForExit();
                return result.Trim();
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
                return result.Trim();
            }
        }
    }
    public static string GetSafeFilename(string filename)
    {
        return string.Join(String.Empty, filename.Split(Path.GetInvalidFileNameChars())).Trim();
    }
    public static string GetPreferredFilename(string file)
    {
        string filename = SoftWare(@"src\tools\mediainfo.exe", $"\"{file}\" --Inform=General;%Album%", false);
        try
        {
            var _filename = Get.AaxInformation(file);
            var title = _filename[0];
            _filename[0] = _filename[0].Trim().Replace(":", " -");
            string placeholder = _filename[1].Replace("(Unabridged)", string.Empty);
            if (placeholder.Length < 2)
                placeholder = string.Concat("0", placeholder);
            var _file = $"{_filename[2]} [{placeholder}] {_filename[3]}";
            filename = _file.Trim();

        }
        catch
        {
            filename = filename.Trim().Replace(":", " -");
        }
        return GetSafeFilename(filename);
    }
}