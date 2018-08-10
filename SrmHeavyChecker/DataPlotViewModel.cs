using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using OxyPlot;
using Q4SRM.Data;
using Q4SRM.Output;
using ReactiveUI;

namespace SrmHeavyChecker
{
    public class DataPlotViewModel : ReactiveObject
    {
        private string resultsFilePath;
        private readonly Action<string> statusUpdate;
        private PlotModel dataPlot;

        public string ResultsFilePath
        {
            get => resultsFilePath;
            set => this.RaiseAndSetIfChanged(ref resultsFilePath, value);
        }

        public PlotModel DataPlot
        {
            get => dataPlot;
            set => this.RaiseAndSetIfChanged(ref dataPlot, value);
        }

        public ReactiveCommand<Unit, Unit> BrowseForResultsFileCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenResultsFileCommand { get; }


        [Obsolete("For WPF Design view only", true)]
        public DataPlotViewModel() : this(Console.WriteLine)
        {
        }

        public DataPlotViewModel(Action<string> statusUpdater)
        {
            statusUpdate = statusUpdater;

            BrowseForResultsFileCommand = ReactiveCommand.CreateFromTask(BrowseForResultsFile);
            OpenResultsFileCommand = ReactiveCommand.CreateFromTask(OpenResultsFile, this.WhenAnyValue(x => x.ResultsFilePath).Select(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x)));
        }

        private async Task BrowseForResultsFile()
        {
            var dialog = new CommonOpenFileDialog
            {
                Filters = { new CommonFileDialogFilter("Tab-separated text", "*.tsv;*.txt") },
            };

            if (!string.IsNullOrWhiteSpace(ResultsFilePath))
            {
                var dir = Path.GetDirectoryName(ResultsFilePath);
                if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                {
                    dialog.InitialDirectory = dir;
                }
            }

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                ResultsFilePath = dialog.FileName;

                if (!string.IsNullOrWhiteSpace(ResultsFilePath))
                {
                    await OpenResultsFile();
                }
            }
        }

        private async Task OpenResultsFile()
        {
            if (string.IsNullOrWhiteSpace(ResultsFilePath) || !File.Exists(ResultsFilePath))
            {
                statusUpdate($"Error: file \"{ResultsFilePath}\" does not exist.");
                return;
            }

            List<CompoundData> results;

            try
            {
                results = await Task.Run(() => CompoundData.ReadCombinedResultsFile(ResultsFilePath).ToList());
            }
            catch (Exception e)
            {
                statusUpdate($"Error: failed to load file. {e.Message}");
                return;
            }

            DataPlot = Plotting.CreatePlot(results, Path.GetFileName(ResultsFilePath));
        }
    }
}
