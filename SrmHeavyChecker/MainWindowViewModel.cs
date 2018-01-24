using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;

namespace SrmHeavyChecker
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly List<DatasetInfo> datasets = new List<DatasetInfo>();
        private string workFolder;
        private bool excludeArchived = true;
        private bool workFolderRecurse;
        private bool isNotRunning;
        private string status;
        private string validationError;
        private ObservableAsPropertyHelper<bool> statusIsError;
        private CancellationTokenSource cancellationTokenSrc = new CancellationTokenSource();
        public GuiOptions Options { get; } = new GuiOptions();
        public DatasetGridViewModel AvailableDatasetsViewModel { get; } = new DatasetGridViewModel();
        public DatasetGridViewModel DatasetsToProcessViewModel { get; } = new DatasetGridViewModel();
        public DatasetGridViewModel ProcessingQueueViewModel { get; } = new DatasetGridViewModel();

        public string WorkFolder
        {
            get { return workFolder; }
            set
            {
                var oldValue = workFolder;
                this.RaiseAndSetIfChanged(ref workFolder, value);
                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    Options.UpdateOutputFolder(oldValue, value);
                    Options.UpdateSummaryStatsFilePath(oldValue, value);
                });
            }
        }

        public bool WorkFolderRecurse
        {
            get { return workFolderRecurse; }
            set { this.RaiseAndSetIfChanged(ref workFolderRecurse, value); }
        }

        public bool ExcludeArchived
        {
            get { return excludeArchived; }
            set { this.RaiseAndSetIfChanged(ref excludeArchived, value); }
        }

        public bool IsNotRunning
        {
            get { return isNotRunning; }
            set { this.RaiseAndSetIfChanged(ref isNotRunning, value); }
        }

        public string Status
        {
            get { return status; }
            set
            {
                this.RaiseAndSetIfChanged(ref status, value);

                // Clear the status after 10 seconds
                statusClearTimer.Change(10000, Timeout.Infinite);
            }
        }

        public string ValidationError
        {
            get { return validationError; }
            set { this.RaiseAndSetIfChanged(ref validationError, value); }
        }

        private readonly Timer statusClearTimer;

        public bool StatusIsError => statusIsError?.Value ?? false;

        public ReactiveCommand<Unit, Unit> BrowseForFolderCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RefreshDatasetsCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ResetDatasetsCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> MoveToQueueCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RemoveFromQueueCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> BrowseForOutputFolderCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> BrowseForStatsFileCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> BrowseForThresholdsFileCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> BrowseForThresholdsOutputFileCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> ProcessDatasetsCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; private set; }

        public MainWindowViewModel()
        {
            statusClearTimer = new Timer(ClearStatus, this, Timeout.Infinite, Timeout.Infinite);

            BrowseForFolderCommand = ReactiveCommand.Create(() => BrowseForFolder());
            RefreshDatasetsCommand = ReactiveCommand.Create(() => RefreshDatasets());
            ResetDatasetsCommand = ReactiveCommand.Create(() => ResetDatasets());
            MoveToQueueCommand = ReactiveCommand.Create(() => MoveToProcessingList(), this.WhenAnyValue(x => x.AvailableDatasetsViewModel.SelectedData.Count).Select(x => x > 0));
            RemoveFromQueueCommand = ReactiveCommand.Create(() => RemoveFromProcessingList(), this.WhenAnyValue(x => x.DatasetsToProcessViewModel.SelectedData.Count).Select(x => x > 0));
            BrowseForOutputFolderCommand = ReactiveCommand.Create(() => BrowseForOutputFolder());
            BrowseForStatsFileCommand = ReactiveCommand.Create(() => BrowseForStatsFile());
            BrowseForThresholdsFileCommand = ReactiveCommand.Create(() => BrowseForThresholdFile());
            BrowseForThresholdsOutputFileCommand = ReactiveCommand.Create(() => BrowseForThresholdOutputFilePath());
            ProcessDatasetsCommand = ReactiveCommand.CreateFromTask(() => ProcessDatasets(), this.WhenAnyValue(x => x.AvailableDatasetsViewModel.Data.Count, x => x.DatasetsToProcessViewModel.Data.Count).Select(x => x.Item1 > 0 || x.Item2 > 0));
            CancelCommand = ReactiveCommand.Create(() => CancelProcessing(), this.WhenAnyValue(x => x.IsNotRunning).Select(x => !x));

            this.WhenAnyValue(x => x.WorkFolderRecurse, x => x.ExcludeArchived).Subscribe(x => LoadDatasets());
            this.WhenAnyValue(x => x.Status).Select(x => x.ToLower().Contains("error")).ToProperty(this, x => x.StatusIsError, out statusIsError, false);
            IsNotRunning = true;
        }

        private void CancelProcessing()
        {
            cancellationTokenSrc.Cancel();
            Status = "Cancelling processing after current tasks complete...";
        }

        private async Task ProcessDatasets()
        {
            Options.FilesToProcessList.Clear();
            if (DatasetsToProcessViewModel.AllData.Count > 0)
            {
                Options.FilesToProcessList.AddRange(DatasetsToProcessViewModel.AllData.Select(x => x.DatasetPath));
            }
            else
            {
                Options.FilesToProcessList.AddRange(AvailableDatasetsViewModel.Data.Select(x => x.DatasetPath));
            }

            ValidationError = Options.Validate();

            if (!string.IsNullOrWhiteSpace(ValidationError))
            {
                return;
            }

            IsNotRunning = false;
            var processor = new FileProcessor();
            await Task.Run(() => processor.RunProcessing(Options, cancellationTokenSrc));
            IsNotRunning = true;
            Status = "Finished processing datasets.";
        }

        private void BrowseForFolder()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var folder = dialog.FileName;
                if (folder.ToLower().EndsWith(".d"))
                {
                    folder = Path.GetDirectoryName(folder);
                }

                WorkFolder = folder;
            }

            LoadDatasets();
        }

        private void BrowseForOutputFolder()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var folder = dialog.FileName;
                if (folder.ToLower().EndsWith(".d"))
                {
                    folder = Path.GetDirectoryName(folder);
                }

                Options.OutputFolder = folder;
                Options.UseOutputFolder = true;
            }
        }

        private void BrowseForStatsFile()
        {
            var dialog = new CommonOpenFileDialog
            {
                DefaultFileName = GuiOptions.SummaryStatsFileDefaultName,
                Filters = { new CommonFileDialogFilter("Tab-separated text", "*.tsv;*.txt") },
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var filePath = dialog.FileName;

                Options.SummaryStatsFilePath = filePath;
            }
        }

        private void BrowseForThresholdFile()
        {
            var dialog = new CommonOpenFileDialog
            {
                Filters = { new CommonFileDialogFilter("Tab-separated text", "*.tsv;*.txt") },
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var filePath = dialog.FileName;

                Options.CompoundThresholdFilePath = filePath;
                Options.UseCompoundThresholdsFile = true;
            }
        }

        private void BrowseForThresholdOutputFilePath()
        {
            var dialog = new CommonOpenFileDialog
            {
                Filters = { new CommonFileDialogFilter("Tab-separated text", "*.tsv;*.txt") },
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var filePath = dialog.FileName;

                Options.CompoundThresholdOutputFilePath = filePath;
            }
        }

        private void LoadDatasets()
        {
            this.datasets.Clear();
            datasets.AddRange(GetDatasetsFromDisk().OrderBy(x => x.DatasetName));

            using (AvailableDatasetsViewModel.AllData.SuppressChangeNotifications())
            using (DatasetsToProcessViewModel.AllData.SuppressChangeNotifications())
            {
                AvailableDatasetsViewModel.Clear();
                DatasetsToProcessViewModel.Clear();
                AvailableDatasetsViewModel.AllData.AddRange(datasets);
            }

            Status = $"Loaded {datasets.Count} datasets from disk.";
        }

        private List<DatasetInfo> GetDatasetsFromDisk()
        {
            if (string.IsNullOrWhiteSpace(WorkFolder))
            {
                return new List<DatasetInfo>();
            }

            var searchOption = WorkFolderRecurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var datasetPaths = Directory.EnumerateFiles(WorkFolder, "*.raw", searchOption);

            if (ExcludeArchived)
            {
                datasetPaths = datasetPaths.Where(x => !Path.GetFileNameWithoutExtension(x).ToLower().StartsWith("x_"));
            }

            return datasetPaths.Select(x => new DatasetInfo(x)).ToList();
        }

        private void RefreshDatasets()
        {
            var toUpdateDatasetNames = DatasetsToProcessViewModel.Data.Select(x => x.DatasetName).ToList();

            LoadDatasets();

            using (AvailableDatasetsViewModel.AllData.SuppressChangeNotifications())
            using (DatasetsToProcessViewModel.AllData.SuppressChangeNotifications())
            {
                foreach (var dataset in AvailableDatasetsViewModel.Data)
                {
                    if (toUpdateDatasetNames.Contains(dataset.DatasetName))
                    {
                        DatasetsToProcessViewModel.AllData.Add(dataset);
                    }
                }

                AvailableDatasetsViewModel.AllData.RemoveAll(DatasetsToProcessViewModel.AllData);
            }
        }

        private void ResetDatasets()
        {
            using (AvailableDatasetsViewModel.AllData.SuppressChangeNotifications())
            using (DatasetsToProcessViewModel.AllData.SuppressChangeNotifications())
            {
                AvailableDatasetsViewModel.Clear();
                DatasetsToProcessViewModel.Clear();
                AvailableDatasetsViewModel.AllData.AddRange(datasets);
            }
        }

        private void MoveToProcessingList()
        {
            var datasetsToMove = AvailableDatasetsViewModel.SelectedData.ToList();
            if (datasetsToMove.Count == 0)
            {
                return;
            }

            using (AvailableDatasetsViewModel.AllData.SuppressChangeNotifications())
            using (DatasetsToProcessViewModel.AllData.SuppressChangeNotifications())
            {
                DatasetsToProcessViewModel.AllData.AddRange(datasetsToMove);
                AvailableDatasetsViewModel.AllData.RemoveAll(datasetsToMove);
            }
        }

        private void RemoveFromProcessingList()
        {
            var datasetsToMove = DatasetsToProcessViewModel.SelectedData.ToList();
            if (datasetsToMove.Count == 0)
            {
                return;
            }

            using (AvailableDatasetsViewModel.AllData.SuppressChangeNotifications())
            using (DatasetsToProcessViewModel.AllData.SuppressChangeNotifications())
            {
                AvailableDatasetsViewModel.AllData.AddRange(datasetsToMove);
                DatasetsToProcessViewModel.AllData.RemoveAll(datasetsToMove);
            }
        }

        private void ClearStatus(object obj)
        {
            Status = "";
        }
    }
}
