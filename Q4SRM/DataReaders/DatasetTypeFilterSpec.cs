using System;

namespace Q4SRM.DataReaders
{
    public class DatasetTypeFilterSpec : IEquatable<DatasetTypeFilterSpec>
    {
        public bool IsFolderDataset { get; }
        public string Description { get; }
        public string WildcardSpec { get; }
        public string PathEnding { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pathEnding">End of dataset name (extension), excluding '.'</param>
        /// <param name="wildcardSpec"></param>
        /// <param name="description"></param>
        /// <param name="isFolderDataset"></param>
        public DatasetTypeFilterSpec(string pathEnding, string wildcardSpec = null, string description = "", bool isFolderDataset = false)
        {
            PathEnding = pathEnding;
            WildcardSpec = wildcardSpec;
            if (string.IsNullOrWhiteSpace(wildcardSpec))
            {
                WildcardSpec = "*." + PathEnding;
            }

            Description = description;
            IsFolderDataset = isFolderDataset;
        }

        public bool Equals(DatasetTypeFilterSpec other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IsFolderDataset == other.IsFolderDataset && string.Equals(WildcardSpec, other.WildcardSpec) && string.Equals(PathEnding, other.PathEnding);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DatasetTypeFilterSpec) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsFolderDataset.GetHashCode();
                hashCode = (hashCode * 397) ^ (WildcardSpec != null ? WildcardSpec.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PathEnding != null ? PathEnding.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
