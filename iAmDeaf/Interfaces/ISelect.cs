using Newtonsoft.Json.Linq;

namespace iAmDeaf.Interfaces
{
    internal class ISelect
    {
        internal static IAudiobook Load(string aax)
        {
            switch (((string)(dynamic)JObject.Parse(File.ReadAllText("src\\configuration.json"))["codec"]).ToLower())
            {
                case "m4b":
                    {
                        return new Codecs.M4B();
                    }

                case "mp3":
                    {
                        return new Codecs.MP3();
                    }

                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }
    }
}
