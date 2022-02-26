using System.Text;
using System.Diagnostics;

internal class Other
{
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
}