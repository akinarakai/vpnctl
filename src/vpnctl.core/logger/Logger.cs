public static class Logger
{
    private const string SuccessPrefix = "SUCCESS: ";
    private const string ErrorPrefix   = "ERROR:   ";
    private const string WarnPrefix    = "WARNING: ";
    private const string InfoPrefix    = "INFO:    ";

    public static void Success(string message)
    {
        Render(SuccessPrefix + message, ConsoleColor.Green);
    }

    public static void Error(string message)
    {
        Render(ErrorPrefix + message, ConsoleColor.Red);
    }

    public static void Warn(string message)
    {
        Render(WarnPrefix + message, ConsoleColor.Yellow);
    }

    public static void Info(string message)
    {
        Render(InfoPrefix + message, ConsoleColor.Cyan);
    }

    public static void Text(string message)
    {
        Render(message, ConsoleColor.White);
    }

    public static void Text()
    {
        Render(string.Empty, ConsoleColor.White);
    }

    private static void Render(string text, ConsoleColor color)
    {
        var oldColor = Console.ForegroundColor;
        
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        
        Console.ForegroundColor = oldColor;
    }
}