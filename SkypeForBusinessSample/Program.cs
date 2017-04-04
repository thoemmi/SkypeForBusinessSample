using System.IO;

using NLog;
using NLog.Config;
using NLog.Targets;

using Topshelf;

namespace SkypeForBusinessSample {
    internal class Program {
        private static void Main(string[] args) {
            ConfigureNLog();

            HostFactory.Run(x => {
                x.UseNLog(new LogFactory(LogManager.Configuration));

                x.Service<LyncService>(s => {
                    s.ConstructUsing(name => new LyncService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsPrompt();
            });
        }

        private static void ConfigureNLog() {
            var config = new LoggingConfiguration();

            // log to console
            var consoleTarget = new ColoredConsoleTarget {
                Layout = @"${date:format=HH\:mm\:ss} [${logger}] ${message}"
            };
            config.AddTarget("console", consoleTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, consoleTarget));

            // log to file
            var fileTarget = new FileTarget {
                FileName = Path.Combine(Path.GetTempPath(), "SkypeForBusinessSample.txt"),
            };
            config.AddTarget("file", fileTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));

            // log via 
            var debuggerTarget = new DebuggerTarget();
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, debuggerTarget));

            LogManager.Configuration = config;
        }
    }
}