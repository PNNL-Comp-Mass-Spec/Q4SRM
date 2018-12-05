using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Q4SRM.Data;

namespace Q4SRM.Settings
{
    public static class ISettingsDataExtensions
    {
        public static void SetDefaultThresholds(this ISettingsData settings)
        {
            settings.DefaultIntensityThreshold = 10000;
            settings.EdgeNETThresholdMinutes = 0.5;
            settings.ElutionConcurrenceThresholdMinutes = 0.5;
            settings.SignalToNoiseHeuristicThreshold = 10;
            settings.CheckAllCompounds = false;
        }

        public static void ComputeSha1(this ISettingsData settings)
        {
            if (string.IsNullOrWhiteSpace(settings.CompoundThresholdFilePath) || !File.Exists(settings.CompoundThresholdFilePath))
            {
                settings.CompoundThresholdFileSha1Hash = "";
                return;
            }

            using (var stream = new FileStream(settings.CompoundThresholdFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(stream);
                settings.CompoundThresholdFileSha1Hash = string.Join("", hash.Select(x => x.ToString("x2")));
            }
        }

        public static string ConvertToTsvComment(this ISettingsData settings)
        {
            var formatted = $"DefaultIntensityThreshold: {settings.DefaultIntensityThreshold:0.###}; NET time threshold (min): {settings.EdgeNETThresholdMinutes:0.###}";
            formatted += $"; Elution Concurrence threshold (min): {settings.ElutionConcurrenceThresholdMinutes:0.###}; S/N Heuristic threshold: {settings.SignalToNoiseHeuristicThreshold:0.###}";

            if (!string.IsNullOrWhiteSpace(settings.CompoundThresholdFilePath))
            {
                if (string.IsNullOrWhiteSpace(settings.CompoundThresholdFileSha1Hash))
                {
                    settings.ComputeSha1();
                }
                formatted += $"; CompoundThresholdsFile: \"{settings.CompoundThresholdFilePath}\"; SHA1Hash {settings.CompoundThresholdFileSha1Hash}";
            }

            return formatted;
        }

        public static Dictionary<string, CompoundThresholdData> LoadCompoundThresholds(this ISettingsData settings)
        {
            if (string.IsNullOrWhiteSpace(settings.CompoundThresholdFilePath) || !File.Exists(settings.CompoundThresholdFilePath))
            {
                return null;
            }

            var thresholds = CompoundThresholdData.ReadFromFile(settings.CompoundThresholdFilePath).ToList();
            return CompoundThresholdData.ConvertToSearchMap(thresholds);
        }

        private static readonly Regex TsvCommentSettingsRegex = new Regex(@"DefaultIntensityThreshold: (?<defaultThreshold>\d+\.?\d*); NET time threshold \(min\): (?<netTimeThreshold>\d+\.?\d*); Elution Concurrence threshold \(min\): (?<elutionConcurrenceThreshold>\d+\.?\d*); S/N Heuristic threshold: (?<snHeuristicThreshold>\d+\.?\d*)(; CompoundThresholdsFile: (?<filepath>.*); SHA1Hash (?<fileHash>.*))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void PopulateFromTsvComment(this ISettingsData settings, string tsvCommentLine)
        {
            var match = TsvCommentSettingsRegex.Match(tsvCommentLine);

            var defaultThreshold = match.Groups["defaultThreshold"].Value;
            if (!string.IsNullOrWhiteSpace(defaultThreshold))
            {
                settings.DefaultIntensityThreshold = double.Parse(defaultThreshold);
            }

            var netTimeThreshold = match.Groups["netTimeThreshold"].Value;
            if (!string.IsNullOrWhiteSpace(netTimeThreshold))
            {
                settings.EdgeNETThresholdMinutes = double.Parse(netTimeThreshold);
            }

            var elutionConcurrenceThreshold = match.Groups["elutionConcurrenceThreshold"].Value;
            if (!string.IsNullOrWhiteSpace(elutionConcurrenceThreshold))
            {
                settings.ElutionConcurrenceThresholdMinutes = double.Parse(elutionConcurrenceThreshold);
            }

            var snHeuristicThreshold = match.Groups["snHeuristicThreshold"].Value;
            if (!string.IsNullOrWhiteSpace(snHeuristicThreshold))
            {
                settings.SignalToNoiseHeuristicThreshold = double.Parse(snHeuristicThreshold);
            }

            // if not match, the value is string.Empty. (also .Success will be false)
            settings.CompoundThresholdFilePath = match.Groups["filepath"].Value.Trim().Trim('"', '\'');
            settings.CompoundThresholdFileSha1Hash = match.Groups["fileHash"].Value.Trim();
        }

        public static bool SettingsEquals(this ISettingsData settings1, ISettingsData settings2)
        {
            if (settings1 == null || settings2 == null)
            {
                return false;
            }

            if (!settings1.DefaultIntensityThreshold.Equals(settings2.DefaultIntensityThreshold) ||
                !settings1.EdgeNETThresholdMinutes.Equals(settings2.EdgeNETThresholdMinutes) ||
                !settings1.ElutionConcurrenceThresholdMinutes.Equals(settings2.ElutionConcurrenceThresholdMinutes) ||
                !settings1.SignalToNoiseHeuristicThreshold.Equals(settings2.SignalToNoiseHeuristicThreshold))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings1.CompoundThresholdFileSha1Hash) &&
                string.IsNullOrWhiteSpace(settings1.CompoundThresholdFileSha1Hash) &&
                string.IsNullOrWhiteSpace(settings1.CompoundThresholdFilePath) &&
                string.IsNullOrWhiteSpace(settings1.CompoundThresholdFilePath))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(settings1.CompoundThresholdFileSha1Hash) ||
                string.IsNullOrWhiteSpace(settings1.CompoundThresholdFileSha1Hash))
            {
                return false;
            }

            if (!settings1.CompoundThresholdFileSha1Hash.Equals(settings2.CompoundThresholdFileSha1Hash))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings1.CompoundThresholdFilePath) ||
                string.IsNullOrWhiteSpace(settings1.CompoundThresholdFilePath))
            {
                return false;
            }

            if (settings1.CompoundThresholdFilePath.Equals(settings2.CompoundThresholdFilePath))
            {
                return true;
            }

            // If direct equality was false, try again, comparing only the file names
            if (!Path.GetFileName(settings1.CompoundThresholdFilePath).Equals(Path.GetFileName(settings2.CompoundThresholdFilePath)))
            {
                return false;
            }

            return true;
        }
    }
}
