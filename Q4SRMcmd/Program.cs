using System;
using System.Threading;
using PRISM;
using Q4SRM;

namespace Q4SRMcmd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Run command-line
            var parser = new CommandLineParser<CmdLineOptions>();
            var parsed = parser.ParseArgs(args);
            var options = parsed.ParsedResults;

            if (!parsed.Success || !options.Validate())
            {
                return;
            }

            var cancelTokenSource = new CancellationTokenSource();
            // Handle Ctrl-C in a graceful way
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (cancelTokenSource.IsCancellationRequested)
                {
                    Console.WriteLine(@"Force closing...");
                    return;
                }

                eventArgs.Cancel = true;
                Console.WriteLine(@"Exiting when currently processing item(s) complete. Press Ctrl-C again to kill now.");
                cancelTokenSource.Cancel();
            };

            var processor = new FileProcessor();
            processor.RunProcessing(options, cancelTokenSource);
        }
    }
}
