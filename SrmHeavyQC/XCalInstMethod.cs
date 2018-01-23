using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SrmHeavyQC
{
    public class XCalInstMethod
    {
        public XCalInstMethod(string method)
        {
            DirtyMethod = method;
            UsesCompoundName = false;
            ProcessMethod(method);
        }

        public string DirtyMethod { get; private set; }
        public string Method => CleanedMethod;
        public string CleanedMethod { get; private set; }
        public string SrmTable { get; private set; }

        public bool UsesCompoundName { get; private set; }

        public List<TransitionData> ParseSrmTable()
        {
            var bytes = Encoding.ASCII.GetBytes(SrmTable);
            var memStream = new MemoryStream(bytes);
            return TransitionData.ParseSrmTable(memStream).ToList();
        }

        private void ProcessMethod(string method)
        {
            using (var memStream = new MemoryStream(method.Length * 2))
            using (var memStreamSrm = new MemoryStream(method.Length * 2))
            {
                using (var stream = new StreamWriter(memStream, Encoding.ASCII, 65535, true))
                using (var streamSrm = new StreamWriter(memStreamSrm, Encoding.ASCII, 65535, true))
                {
                    var altisTableTabRegex = new Regex(@" \| *", RegexOptions.Compiled);
                    var vantageTableTabRegex = new Regex(@" +", RegexOptions.Compiled);

                    var split = method.Split('\n');
                    var inTable = false;
                    var isAltis = false;
                    for (var i = 0; i < split.Length; i++)
                    {
                        var line = split[i];
                        if (i + 1 < split.Length)
                        {
                            var next = split[i + 1];
                            if (next.StartsWith("                              "))
                            {
                                // Need to undo some line wrapping
                                line += next.TrimStart(' ');
                                i++;
                            }
                        }

                        if (line.Contains("TSQ Altis"))
                        {
                            isAltis = true;
                        }

                        line = line.Replace("\r", "");

                        if (line.Trim().StartsWith("Compound Name"))
                        {
                            UsesCompoundName = true;
                            // Check for table with Altis
                            inTable = true;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            inTable = false;
                            line = "";
                        }
                        else if (inTable)
                        {
                            var trimmed = line.TrimStart(' ');
                            var tabbed = "";
                            if (isAltis)
                            {
                                tabbed = altisTableTabRegex.Replace(trimmed, "\t");
                                // Warning: Thermo software places thousands separators in the data from Altis...
                            }
                            else
                            {
                                tabbed = vantageTableTabRegex.Replace(trimmed, "\t");
                            }

                            line = tabbed;
                            streamSrm.WriteLine(line);
                        }
                        else if (line.Trim().StartsWith("SRM Table"))
                        {
                            // Check for table with Vantage
                            inTable = true;
                        }

                        stream.WriteLine(line);
                    }
                }

                memStream.Seek(0, SeekOrigin.Begin);
                memStreamSrm.Seek(0, SeekOrigin.Begin);

                using (var stream = new StreamReader(memStream))
                using (var streamSrm = new StreamReader(memStreamSrm))
                {
                    CleanedMethod = stream.ReadToEnd();
                    SrmTable = streamSrm.ReadToEnd();
                }
            }
        }
    }
}
