using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.Threading;

namespace Net.Myzuc.Minecraft.Server
{
    public static class Logs
    {
        public enum LogLevel
        {
            Debug,
            Verbose,
            Information,
            Warning,
            Error,
        }
        public record LogMessage
        {
            public string Origin;
            public string Message;
            public LogLevel Level;
            public LogMessage(string origin, string message, LogLevel level)
            {
                Origin = origin;
                Message = message;
                Level = level;
            }
        }
        private static readonly AsyncQueue<LogMessage> Queue = new();
        public static event EventHandler<LogMessage> OnLine = (sender, args) => { };
        static Logs()
        {
            try
            {
                OnLine += (sender, args) =>
                {
                    ConsoleColor color = args.Level switch
                    {
                        LogLevel.Debug => ConsoleColor.DarkMagenta,
                        LogLevel.Verbose => ConsoleColor.DarkGray,
                        LogLevel.Information => ConsoleColor.Blue,
                        LogLevel.Warning => ConsoleColor.DarkYellow,
                        LogLevel.Error => ConsoleColor.DarkRed,
                        _ => ConsoleColor.White,
                    };
                    bool error = args.Level is LogLevel.Error or LogLevel.Warning;
                    Console.ForegroundColor = color;
                    string line = $"[{args.Level}][{args.Origin}]: {args.Message}";
                    (error ? Console.Error : Console.Out).WriteLine(line);
                };
                _ = Task.Run(async () =>
                {
                    try
                    {
                        while (true)
                        {
                            OnLine(null, await Queue.DequeueAsync());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error while logging: {ex}");
                    }
                });
                try
                {
                    string directory = Path.Combine(Environment.CurrentDirectory, "Logs");
                    Directory.CreateDirectory(directory);
                    string date = $"{DateTime.Now:yyyy-MM-dd}";
                    string path;
                    int number = 0;
                    do path = Path.Combine(directory, $"{date}_{++number}.log");
                    while(File.Exists(path));
                    FileStream fs = File.Create(path);
                    OnLine += (sender, args) =>
                    {
                        string line = $"[{DateTime.Now:yyyy/MM/dd-hh:mm:ss}] [{args.Level}][{args.Origin}]: {args.Message}{Environment.NewLine}";
                        fs.Write(Encoding.UTF8.GetBytes(line));
                        fs.Flush();
                    };
                    AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                    {
                        fs.Flush();
                        fs.Dispose();
                    };
                }
                catch (Exception ex)
                {
                    Logs.Error($"Error while opening log file: {ex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while initializing logger: {ex}");
            }
        }
        public static void Debug(string message)
        {
            Message(LogLevel.Debug, Assembly.GetCallingAssembly(), message);
        }
        public static void Verbose(string message)
        {
            Message(LogLevel.Verbose, Assembly.GetCallingAssembly(), message);
        }
        public static void Info(string message)
        {
            Message(LogLevel.Information, Assembly.GetCallingAssembly(), message);
        }
        public static void Warning(string message)
        {
            Message(LogLevel.Warning, Assembly.GetCallingAssembly(), message);
        }
        public static void Error(string message)
        {
            Message(LogLevel.Error, Assembly.GetCallingAssembly(), message);
        }
        public static void Log(LogLevel level, string message)
        {
            Message(level, Assembly.GetCallingAssembly(), message);
        }
        private static void Message(LogLevel level, Assembly assembly, string message)
        {
            Queue.Enqueue(new(assembly.GetName().Name ?? string.Empty, message, level));
        }
    }
}