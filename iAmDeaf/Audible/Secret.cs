using Aax.Activation.ApiClient;
using System.Text;

namespace iAmDeaf.Audible
{
    internal class Secret
    {
        internal static async Task<string> aaxcAsync(string checksum)
        {
            return AaxActivationClient.Instance.ResolveActivationBytes(checksum).Result.ToString();
        }
        internal static string Hash(string file)
        {
            try
            {
                using (var fs = System.IO.File.OpenRead(file))
                using (var br = new BinaryReader(fs))
                {
                    fs.Position = 0x251 + 56 + 4;
                    var checksum = br.ReadBytes(20);
                    return bytes(checksum);
                }
            }
            catch (Exception ex)
            {
                Alert.Error("Error calculating Hash: "+ ex.Message);
            }
            return String.Empty;
        }
        internal static string bytes(byte[] bt)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bt)
                    sb.Append(b.ToString("X2"));

                string hexString = sb.ToString();
                return hexString;
            }
            catch (Exception ex)
            {
                Alert.Error("Converting Hash to Hex: "+ex.Message);
                return String.Empty;
            }
        }
        public static string GetBytesFromFile(string aax)
        {
            var checksum = Hash(aax);
            string[] keys = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\data\KeyHistory\log"));

            for (int i = 0; i < keys.Length; i+=2)
            {
                if (keys[i] == checksum)
                {
                    return keys[i+1];
                }
            }

            string bytes = string.Empty;
            try
            {
                var h = new AAXHash.Data();
                h.Reverse(aax);
                bytes = h.bytes;
            }
            catch
            {
                try
                {
                    bytes = AaxActivationClient.Instance.ResolveActivationBytes(checksum).Result.ToString();
                }
                catch (Exception ex)
                {
                    Alert.Error("Key not found in offline log: "+ex.Message);
                    return string.Empty;
                }
            }

            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"src\data\KeyHistory\log"), $"{checksum}\n{bytes}" + Environment.NewLine);
            return bytes;
        }
    }
}