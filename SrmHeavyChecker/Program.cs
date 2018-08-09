using System;
using System.Runtime.InteropServices;
using System.Threading;
using PRISM;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        //[STAThread]
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                FreeConsole();
                // Run GUI
                var thread = new Thread(() => { new App().Run(); });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                return;
            }

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
