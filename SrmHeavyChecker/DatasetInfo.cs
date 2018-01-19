using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace SrmHeavyChecker
{
    public class DatasetInfo : ReactiveObject
    {
        private string updateNote = "";
        private bool datasetUpdated = false;
        private bool processingWarning = false;
        private bool updateError = false;
        public string DatasetName { get; }
        public string DatasetPath { get; }
        public string DatasetFolder { get; }
        public DateTime AcquisitionDate { get; }

        public string UpdateNote
        {
            get { return updateNote; }
            set { this.RaiseAndSetIfChanged(ref updateNote, value); }
        }

        public bool DatasetUpdated
        {
            get { return datasetUpdated; }
            set { this.RaiseAndSetIfChanged(ref datasetUpdated, value); }
        }

        public bool ProcessingWarning
        {
            get { return processingWarning; }
            set { this.RaiseAndSetIfChanged(ref processingWarning, value); }
        }

        public bool UpdateError
        {
            get { return updateError; }
            set { this.RaiseAndSetIfChanged(ref updateError, value); }
        }

        public DatasetInfo(string path)
        {
            DatasetPath = path;
            DatasetFolder = Path.GetDirectoryName(path);
            DatasetName = Path.GetFileNameWithoutExtension(path);
        }

        [Obsolete("For WPF design time use only", true)]
        public DatasetInfo(string name, DateTime date)
        {
            DatasetName = name;
            AcquisitionDate = date;
        }
    }
}
