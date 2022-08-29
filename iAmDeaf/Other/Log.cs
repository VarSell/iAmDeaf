namespace iAmDeaf.Other
{
    internal static class Record
    {
        public static void Log(Exception ex, System.Diagnostics.StackTrace st)
        {
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"\rec.log");

            try
            {
                string date = DateTime.Now.ToString("MMMM-dd-yyyy HH:mm:ss");
                var x = st.GetFrame(0);

                string classCrash = x.GetMethod().DeclaringType.ToString();
                string methodCrash = x.GetMethod().ToString();
                string lnCrash = x.GetFileLineNumber().ToString();
                string clnCrash = x.GetFileColumnNumber().ToString();

                string _t = $"\n{new string('*', date.Length+4)}\n* {date} *\n{new string('*', date.Length+4)}\n\nClass  : {classCrash}\nMethod : {methodCrash}\nLine   : {lnCrash}\nColumn : {clnCrash}\n\nMessage\n{ex.Message}\n\nStackTrace\n{ex.StackTrace}".Trim();
                string msg = Environment.NewLine+_t+Environment.NewLine;
                if (!File.Exists(logFile))
                {
                    File.WriteAllText(logFile, msg);
                }
                else
                {
                    File.AppendAllText(logFile, msg);
                }
                Alert.Error(string.Concat(x.GetMethod().Name, ": ", ex.Message));
            }
            catch (Exception e)
            {
                Alert.Error("Log Crash");
                Console.Error.WriteLine(e.ToString());
                Console.ReadKey();
            }
        }
    }
}
