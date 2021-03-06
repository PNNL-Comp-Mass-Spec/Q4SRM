﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAPICodePack.Dialogs;
using Q4SRM;
using Q4SRM.DataReaders;
using Q4SRM.Output;
using ReactiveUI;

namespace Q4SRMui
{
    public class DataProcessingViewModel : ReactiveObject
    {
        private readonly List<DatasetInfo> datasets = new List<DatasetInfo>();
        private string workFolder;
        private bool excludeArchived = true;
        private bool workFolderRecurse;
        private bool isNotRunning;
        private string validationError;
        private readonly CancellationTokenSource cancellationTokenSrc = new CancellationTokenSource();
        public GuiOptions Options { get; } = new GuiOptions();
        public DatasetGridViewModel AvailableDatasetsViewModel { get; } = new DatasetGridViewModel();
        public DatasetGridViewModel DatasetsToProcessViewModel { get; } = new DatasetGridViewModel();
        public DatasetGridViewModel ProcessingQueueViewModel { get; } = new DatasetGridViewModel();
        public IReadOnlyReactiveList<Plotting.ExportFormat> ExportFormats { get; }

        public string WorkFolder
        {
            get => workFolder;
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
            get => workFolderRecurse;
            set => this.RaiseAndSetIfChanged(ref workFolderRecurse, value);
        }

        public bool ExcludeArchived
        {
            get => excludeArchived;
            set => this.RaiseAndSetIfChanged(ref excludeArchived, value);
        }

        public bool IsNotRunning
        {
            get => isNotRunning;
            set => this.RaiseAndSetIfChanged(ref isNotRunning, value);
        }

        public string ValidationError
        {
            get => validationError;
            set => this.RaiseAndSetIfChanged(ref validationError, value);
        }

        public ReactiveCommand<Unit, Unit> BrowseForFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> RefreshDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> MoveToQueueCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveFromQueueCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseForOutputFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseForMethodFileCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseForStatsFileCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseForThresholdsFileCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseForThresholdsOutputFileCommand { get; }
        public ReactiveCommand<Unit, Unit> ProcessDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        private readonly Action<string> statusUpdate;

        [Obsolete("For WPF Design view only", true)]
        public DataProcessingViewModel() : this(Console.WriteLine)
        {
        }

        public DataProcessingViewModel(Action<string> statusUpdater)
        {
            statusUpdate = statusUpdater;

            BrowseForFolderCommand = ReactiveCommand.CreateFromTask(BrowseForFolder);
            RefreshDatasetsCommand = ReactiveCommand.CreateFromTask(RefreshDatasets);
            ResetDatasetsCommand = ReactiveCommand.Create(ResetDatasets);
            MoveToQueueCommand = ReactiveCommand.Create(MoveToProcessingList, this.WhenAnyValue(x => x.AvailableDatasetsViewModel.SelectedData.Count).Select(x => x > 0));
            RemoveFromQueueCommand = ReactiveCommand.Create(RemoveFromProcessingList, this.WhenAnyValue(x => x.DatasetsToProcessViewModel.SelectedData.Count).Select(x => x > 0));
            BrowseForOutputFolderCommand = ReactiveCommand.Create(BrowseForOutputFolder);
            BrowseForMethodFileCommand = ReactiveCommand.Create(BrowseForMethodFile);
            BrowseForStatsFileCommand = ReactiveCommand.Create(BrowseForStatsFile);
            BrowseForThresholdsFileCommand = ReactiveCommand.Create(BrowseForThresholdFile);
            BrowseForThresholdsOutputFileCommand = ReactiveCommand.Create(BrowseForThresholdOutputFilePath);
            ProcessDatasetsCommand = ReactiveCommand.CreateFromTask(ProcessDatasets, this.WhenAnyValue(x => x.AvailableDatasetsViewModel.Data.Count, x => x.DatasetsToProcessViewModel.Data.Count).Select(x => x.Item1 > 0 || x.Item2 > 0));
            CancelCommand = ReactiveCommand.Create(CancelProcessing, this.WhenAnyValue(x => x.IsNotRunning).Select(x => !x));

            // Trigger the loading of plugins.
            ReaderLoader.GetFilterSpecs();

            this.WhenAnyValue(x => x.WorkFolderRecurse, x => x.ExcludeArchived).Subscribe(async x => await LoadDatasets());
            IsNotRunning = true;

            ExportFormats = new ReactiveList<Plotting.ExportFormat>(Enum.GetValues(typeof(Plotting.ExportFormat)).Cast<Plotting.ExportFormat>());
        }

        private void CancelProcessing()
        {
            cancellationTokenSrc.Cancel();
            statusUpdate("Cancelling processing after current tasks complete...");
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
            await Task.Run(() => processor.RunProcessing(Options, cancellationTokenSrc, x => RxApp.MainThreadScheduler.Schedule(() => statusUpdate(x))));
            IsNotRunning = true;
            statusUpdate("Finished processing datasets.");
        }

        private async Task BrowseForFolder()
        {
            var dialog = new CommonOpenFolderDialog
            {
                //IsFolderPicker = true,
                ShowFiles = true,
            };

            var filterSpecs = ReaderLoader.GetFilterSpecs();
            var supportedExtensions = string.Join(";", filterSpecs.Where(x => !x.IsFolderDataset).Select(x => x.WildcardSpec));
            dialog.Filters.Add(new CommonFileDialogFilter("SRM datasets", supportedExtensions));
            var folderDatasetTypes = filterSpecs.Where(x => x.IsFolderDataset).ToList();

            dialog.FolderChanging += (sender, args) =>
            {
                if (folderDatasetTypes.Any(x => args.Folder.EndsWith(x.PathEnding, StringComparison.OrdinalIgnoreCase)))
                {
                    // Prevent navigating inside folder datasets
                    args.Cancel = true;
                }
            };

            if (!string.IsNullOrWhiteSpace(WorkFolder) && Directory.Exists(WorkFolder))
            {
                dialog.InitialDirectory = WorkFolder;
            }

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var folder = dialog.FileName;
                if (File.Exists(folder))
                {
                    folder = Path.GetDirectoryName(Path.GetFullPath(folder));
                }

                WorkFolder = folder;
            }

            await LoadDatasets();
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

        private void BrowseForMethodFile()
        {
            var dialog = new CommonOpenFileDialog
            {
                Filters = { new CommonFileDialogFilter("Method File TSV or CSV", ".txt;.tsv;.csv") }
            };

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var file = dialog.FileName;

                Options.MethodFilePath = file;
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

        private async Task LoadDatasets()
        {
            this.datasets.Clear();

            using (AvailableDatasetsViewModel.AllData.SuppressChangeNotifications())
            using (DatasetsToProcessViewModel.AllData.SuppressChangeNotifications())
            {
                AvailableDatasetsViewModel.Clear();
                DatasetsToProcessViewModel.Clear();
            }

            await Task.Run(() =>
            {
                foreach (var dataset in GetDatasetsFromDisk())
                {
                    datasets.Add(dataset);
                    RxApp.MainThreadScheduler.Schedule(x => AvailableDatasetsViewModel.AllData.Add(dataset));
                }
            });

            statusUpdate($"Loaded {datasets.Count} datasets from disk.");
        }

        private IEnumerable<DatasetInfo> GetDatasetsFromDisk()
        {
            if (string.IsNullOrWhiteSpace(WorkFolder))
            {
                return new List<DatasetInfo>();
            }

            /*
            var searchOption = WorkFolderRecurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var datasetPaths = Directory.EnumerateFiles(WorkFolder, "*.raw", searchOption);
            */
            var datasetPaths = ReaderLoader.GetDatasetPathsInPath(WorkFolder, WorkFolderRecurse);

            if (ExcludeArchived)
            {
                datasetPaths = datasetPaths.Where(x => !Path.GetFileNameWithoutExtension(x).ToLower().StartsWith("x_"));
            }

            return datasetPaths.OrderBy(x => Path.GetFileName(x)).Select(x => new DatasetInfo(x)).ToList();
        }

        private async Task RefreshDatasets()
        {
            var toUpdateDatasetNames = DatasetsToProcessViewModel.Data.Select(x => x.DatasetName).ToList();

            await LoadDatasets();

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
    }
}
