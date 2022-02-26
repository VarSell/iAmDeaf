using System.Text;

class Alert
{
    public static void Notify(string alert)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write("  [");
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write(alert);
        Console.ResetColor();
        Console.WriteLine("]");
        Console.OutputEncoding = Encoding.Default;
    }
    public static void Error(string alert)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write("  [Error: ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(alert);
        Console.ResetColor();
        Console.WriteLine("]");
        Console.OutputEncoding = Encoding.Default;
    }
    public static void Success(string alert)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Write("  [");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(alert);
        Console.ResetColor();
        Console.WriteLine("]");
        Console.OutputEncoding = Encoding.Default;
    }
}