using AAXClean;
using iAmDeaf.Interfaces;
using CsAtomReader;
using System.Diagnostics;
using iAmDeaf.Other;

namespace iAmDeaf.Codecs
{
    
    internal class M4B : IAudiobook
    {
        internal string sourceFile { get; set; }
        internal string outFile { get; set; }
        internal AAXClean.AaxFile encryptedFile { get; set; }
        internal string secret { get; set; }
        public bool Open(string file)
        {
            try
            {
                this.sourceFile = file;
                this.outFile = Path.GetDirectoryName(sourceFile);
                using (FileStream stream = new FileStream(sourceFile, FileMode.Open))
                {
                    string aaxTitle = new AtomReader(stream)
                    .GetMetaAtomValue(AtomReader.TitleTypeName)
                    .Replace(":", " -").Replace("?", "");

                    this.outFile = Path.Combine(outFile, string.Concat(aaxTitle, ".m4b"));
                }

                this.encryptedFile = new AAXClean.AaxFile(File.OpenRead(sourceFile));
                if (Path.GetExtension(this.sourceFile) == ".aax")
                {
                    this.secret = Audible.Secret.GetBytesFromFile(sourceFile);
                    this.encryptedFile.SetDecryptionKey(this.secret);
                }
                return true;
            }
            catch (Exception ex)
            {
                Record.Log(ex, new StackTrace(true));
                return false;
            }
        }
        public bool SetPathAndFileName(string file)
        {
            try
            {
                if (string.IsNullOrEmpty(file))
                {
                    Alert.Error("Path cannot be empty.");
                    return false;
                }

                this.outFile = string.Concat(file, ".", this.GetType().Name.ToLower());
                return true;
            }
            catch (Exception ex)
            {
                Record.Log(ex, new StackTrace(true));
                return false;
            }
        }
        public void SetDecryptionKey(string license_key, string license_iv)
        {
            this.encryptedFile.SetDecryptionKey(license_key, license_iv);
        }
        public bool Encode(bool split = false)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                if (split)
                {
                    string splitPath = Path.Combine(Path.GetDirectoryName(this.outFile), Path.GetFileNameWithoutExtension(this.outFile));
                    if (!Directory.Exists(splitPath))
                    {
                        Directory.CreateDirectory(splitPath);
                    }

                    sw.Start();
                    var conversionResult = this.encryptedFile.ConvertToMultiMp4a(this.encryptedFile.GetChapterInfo(), NewSplit);

                    void NewSplit(NewSplitCallback newSplitCallback)
                    {
                        string dir = splitPath;
                        string fileName = newSplitCallback.Chapter.Title.Replace(":", "") + ".m4b";
                        newSplitCallback.OutputFile = File.OpenWrite(Path.Combine(dir, fileName));
                    }
                }
                else
                {
                    sw.Start();
                    this.encryptedFile.ConvertToMp4a(File.Open((this.outFile), FileMode.OpenOrCreate, FileAccess.ReadWrite));
                }

                sw.Stop();
                Alert.Notify(String.Format("Decrypted in {0}ms", sw.ElapsedMilliseconds.ToString()));
                return true;
            }
            catch (Exception ex)
            {
                Record.Log(ex, new StackTrace(true));
                return false;
            }
        }
        public bool Close()
        {
            try
            {
                this.encryptedFile.Close();
                return true;
            }
            catch (Exception ex)
            {
                Record.Log(ex, new StackTrace(true));
                return false;
            }
        }
    }
}
