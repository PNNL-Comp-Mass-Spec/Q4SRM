using System.Reactive.Linq;
using System.Threading;
using ReactiveUI;

namespace Q4SRMui
{
    public class MainWindowViewModel : ReactiveObject
    {
        private string status = "";
        private readonly ObservableAsPropertyHelper<bool> statusIsError;

        public DataProcessingViewModel DataProcessingVm { get; }
        public DataPlotViewModel DataPlotVm { get; }

        public string Status
        {
            get => status;
            set
            {
                this.RaiseAndSetIfChanged(ref status, value);

                // Clear the status after 10 seconds
                statusClearTimer.Change(10000, Timeout.Infinite);
            }
        }

        private readonly Timer statusClearTimer;

        public bool StatusIsError => statusIsError?.Value ?? false;

        public MainWindowViewModel()
        {
            DataProcessingVm = new DataProcessingViewModel(s => Status = s);
            DataPlotVm = new DataPlotViewModel(s => Status = s);

            statusClearTimer = new Timer(ClearStatus, this, Timeout.Infinite, Timeout.Infinite);

            this.WhenAnyValue(x => x.Status).Select(x => x.ToLower().Contains("error")).ToProperty(this, x => x.StatusIsError, out statusIsError, false);
        }

        private void ClearStatus(object obj)
        {
            Status = "";
        }
    }
}
