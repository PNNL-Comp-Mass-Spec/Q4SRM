using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Q4SRM.DataReaders
{
    public static class ReaderLoader
    {
        public static IEnumerable<string> GetDatasetPathsInPath(string path, bool recurse = false)
        {
            var filterSpecs = GetFilterSpecs().ToList();
            foreach (var dirType in filterSpecs.Where(x => x.IsFolderDataset))
            {
                if (path.ToLower().Contains("." + dirType.PathEnding.ToLower() + Path.DirectorySeparatorChar))
                {
                    var index = path.ToLower().IndexOf("." + dirType.PathEnding.ToLower() + Path.DirectorySeparatorChar);
                    yield return path.Substring(0, index + 2 + dirType.PathEnding.Length);
                    yield break;
                }
            }

            foreach (var dataset in GetDatasetsInDirectory(path, recurse, filterSpecs))
            {
                yield return dataset;
            }
        }

        private static IEnumerable<string> GetDatasetsInDirectory(string path, bool recurse, List<DatasetTypeFilterSpec> filterSpecs)
        {
            if (!Directory.Exists(path))
            {
                if (filterSpecs.Where(x => !x.IsFolderDataset).Any(x => path.EndsWith(x.PathEnding, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return path;
                }
            }
            else
            {
                var isDirDataset = false;
                foreach (var dirType in filterSpecs.Where(x => x.IsFolderDataset))
                {
                    if (path.EndsWith(dirType.PathEnding, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return path;
                        isDirDataset = true;
                    }
                }

                if (!isDirDataset)
                {
                    foreach (var filePath in Directory.EnumerateFiles(path))
                    {
                        if (filterSpecs.Where(x => !x.IsFolderDataset).Any(x => filePath.EndsWith(x.PathEnding, StringComparison.OrdinalIgnoreCase)))
                        {
                            yield return filePath;
                        }
                    }

                    if (recurse)
                    {
                        foreach (var dirPath in Directory.EnumerateDirectories(path))
                        {
                            foreach (var datasetPath in GetDatasetsInDirectory(dirPath, true, filterSpecs))
                            {
                                yield return datasetPath;
                            }
                        }
                    }
                }
            }
        }

        public static List<DatasetTypeFilterSpec> GetFilterSpecs()
        {
            var specs = new List<DatasetTypeFilterSpec>();
            foreach (var readerCreator in ReaderCreators)
            {
                specs.AddRange(readerCreator.DatasetFilters);
            }

            return specs.Distinct().ToList();
        }

        public static IInstanceCreator GetReaderForFile(string filePath)
        {
            foreach (var creator in ReaderCreators)
            {
                var error = creator.CanReadDataset(filePath);
                if (error == CanReadError.None)
                {
                    return creator;
                }

                if (error == CanReadError.MissingDependency)
                {
                    // TODO: report this to user!!!
                }
            }

            return null;
        }

        // some code from http://www.freedevelopertutorials.com/csharp-tutorials/plugins-and-modules/
        private static readonly List<IInstanceCreator> ReaderCreators = new List<IInstanceCreator>();

        static ReaderLoader()
        {
            var processorArch = SystemArchitectures.x86;
            if (Environment.Is64BitProcess)
            {
                processorArch = SystemArchitectures.x64;
            }

            // Load the available reader plugins.
            var assembly = Assembly.GetExecutingAssembly();
            var creators = GetReaderCreatorsFromDirectory(Path.GetDirectoryName(assembly.Location));

            ReaderCreators.AddRange(creators.Where(x => x.SupportedArchitectures.HasFlag(processorArch)).OrderBy(x => (int)x.ReaderPriority));
        }

        private static IEnumerable<IInstanceCreator> GetReaderCreatorsFromAssembly(Assembly assembly)
        {
            // Get the classes with implementations of IInstanceCreator
            var types = assembly.GetTypes().Where(x => x.GetInterface(typeof(IInstanceCreator).Name) != null);
            // Create and return instances.
            return types.Select(x => Activator.CreateInstance(x) as IInstanceCreator).Where(x => x != null);
        }

        private static IEnumerable<IInstanceCreator> GetReaderCreatorsFromDirectory(string directoryPath)
        {
            var assemblyPaths = Directory.EnumerateFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly);
            foreach (var assemblyPath in assemblyPaths)
            {
                IEnumerable<IInstanceCreator> creators = new IInstanceCreator[0];
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyPath);
                    creators = GetReaderCreatorsFromAssembly(assembly);
                }
                catch
                {
                    // do nothing?
                }

                foreach (var creator in creators)
                {
                    yield return creator;
                }
            }
        }
    }
}
