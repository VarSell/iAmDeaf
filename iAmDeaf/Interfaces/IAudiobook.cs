using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iAmDeaf.Interfaces
{
    internal interface IAudiobook
    {
        bool Open(string aax);
        bool SetPathAndFileName(string file);
        void SetDecryptionKey(string license_key, string license_iv);
        bool Encode(bool split = false);
        bool Close();
    }
}
