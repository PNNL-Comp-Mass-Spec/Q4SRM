using System;
using System.Reactive.Linq;
using System.Windows.Data;
using ReactiveUI;

namespace SrmHeavyChecker
{
    public class DatasetGridViewModel : ReactiveObject
    {
        private readonly ReactiveList<DatasetInfo> allData = new ReactiveList<DatasetInfo>();
        private readonly ReactiveList<DatasetInfo> selectedData = new ReactiveList<DatasetInfo>();
        private DatasetInfo selectedItem;
        private object dataLock = new object();
        private object selectedDataLock = new object();
        private string filterString;

        public ReactiveList<DatasetInfo> AllData => allData;
        public IReadOnlyReactiveList<DatasetInfo> Data => AllData.CreateDerivedCollection(x => x, x => string.IsNullOrWhiteSpace(FilterString) || x.DatasetName.ToLower().Contains(FilterString.ToLower()), signalReset: this.WhenAnyValue(x => x.FilterString).Throttle(TimeSpan.FromSeconds(0.25)), scheduler:RxApp.MainThreadScheduler);
        public ReactiveList<DatasetInfo> SelectedData => selectedData;

        public DatasetInfo SelectedItem
        {
            get { return selectedItem; }
            set { this.RaiseAndSetIfChanged(ref selectedItem, value); }
        }

        public string FilterString
        {
            get { return filterString; }
            set { this.RaiseAndSetIfChanged(ref filterString, value); }
        }

        public DatasetGridViewModel()
        {
            BindingOperations.EnableCollectionSynchronization(allData, dataLock);
            BindingOperations.EnableCollectionSynchronization(selectedData, selectedDataLock);

            //if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            //{
            //    data.Add(new DatasetInfo("Test1", DateTime.Now));
            //}
        }

        public void Clear()
        {
            AllData.Clear();
            FilterString = "";
            SelectedData.Clear();
            SelectedItem = null;
        }
    }
}
