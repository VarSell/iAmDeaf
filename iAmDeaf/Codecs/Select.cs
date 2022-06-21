using iAmDeaf.Interfaces;
using iAmDeaf.Codecs;

namespace iAmDeaf.Codecs
{
    internal class Select
    {
        public static IAudiobook M4b()
        {
            return new M4B();
        }
        public static IAudiobook Mp3()
        {
            return new MP3();
        }
    }
}
