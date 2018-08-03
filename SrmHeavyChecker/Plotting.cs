using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SrmHeavyQC;

namespace SrmHeavyChecker
{
    public class Plotting
    {
        public const int ExportImageWidth = 1024;
        public const int ExportImageHeight = 768;

        public enum ExportFormat
        {
            [Description("Don't save an image")]
            NoImageExport = -1,
            PNG = 0,
            PDF = 1,
            SVG = 2,
            JPEG = 3,
        }

        public static void PlotResults(List<CompoundData> results, string datasetName, string filepath, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return;
            }

            var plot = CreatePlot(results, datasetName, format);

            switch (format)
            {
                case ExportFormat.PDF:
                    SavePlotToPdf(Path.ChangeExtension(filepath, "pdf"), plot);
                    break;
                case ExportFormat.SVG:
                    SavePlotToSvg(Path.ChangeExtension(filepath, "svg"), plot);
                    break;
                case ExportFormat.PNG:
                    GuaranteeSingleThreadApartment(() => SavePlotToPng(Path.ChangeExtension(filepath, "png"), plot));
                    break;
                case ExportFormat.JPEG:
                    GuaranteeSingleThreadApartment(() => SavePlotToJpg(Path.ChangeExtension(filepath, "jpg"), plot));
                    break;
                default:
                    break;
            }
        }

        public static void PlotCompound(CompoundData result, string filepath, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return;
            }

            var plot = CreateCompoundPlot(result, format);

            switch (format)
            {
                case ExportFormat.PDF:
                    SavePlotToPdf(Path.ChangeExtension(filepath, "pdf"), plot);
                    break;
                case ExportFormat.SVG:
                    SavePlotToSvg(Path.ChangeExtension(filepath, "svg"), plot);
                    break;
                case ExportFormat.PNG:
                    GuaranteeSingleThreadApartment(() => SavePlotToPng(Path.ChangeExtension(filepath, "png"), plot));
                    break;
                case ExportFormat.JPEG:
                    GuaranteeSingleThreadApartment(() => SavePlotToJpg(Path.ChangeExtension(filepath, "jpg"), plot));
                    break;
                default:
                    break;
            }
        }

        public static PlotModel CreateCompoundPlot(CompoundData result, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return null;
            }

            var plot = new OxyPlot.PlotModel()
            {
                TitlePadding = 0,
                Title = result.CompoundName,
                LegendPosition = LegendPosition.RightTop,
                LegendFontSize = 20,
                LegendBorder = OxyColors.Black,
                LegendBorderThickness = 1,
                //LegendItemAlignment = HorizontalAlignment.Right,
                LegendSymbolPlacement = LegendSymbolPlacement.Right,
            };

            var xMax = result.Transitions.Max(x => x.StopTimeMinutes);
            var xMin = result.Transitions.Min(x => x.StartTimeMinutes); ;
            var yMax = result.Transitions.Max(x => x.MaxIntensity);

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity",
                StringFormat = "#e00",
                FontSize = 20,
                Minimum = 0,
                AbsoluteMinimum = 0,
            };

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (min)",
                FontSize = 20,
                Minimum = xMin,
                AbsoluteMinimum = xMin,
                Maximum = xMax,
                AbsoluteMaximum = xMax,
            };

            plot.Axes.Add(yAxis);
            plot.Axes.Add(xAxis);

            var trackerFormatString = $"Scan: {{{nameof(TransitionData.DataPoint.Scan)}}}\nTime: {{{nameof(TransitionData.DataPoint.Time)}}}\nIntensity: {{{nameof(TransitionData.DataPoint.Intensity)}}}";
            var pointMapper = new Func<object, DataPoint>(x => new DataPoint(((TransitionData.DataPoint)x).Time, ((TransitionData.DataPoint)x).Intensity));

            foreach (var transition in result.Transitions)
            {
                var series = new LineSeries()
                {
                    Title = $"{transition.ProductMz:F2}",
                    MarkerType = MarkerType.None,
                    ItemsSource = transition.Intensities,
                    Mapping = pointMapper,
                    TrackerFormatString = trackerFormatString,
                };

                plot.Series.Add(series);
            }

            return plot;
        }

        public static PlotModel CreatePlot(List<CompoundData> results, string datasetName, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return null;
            }

            var passed = results.Where(x => x.PassesIntensity && x.PassesNET).ToList();
            var edge = results.Where(x => x.PassesIntensity && !x.PassesNET).ToList();
            var failed = results.Where(x => !x.PassesIntensity).ToList();

            // TODO: Add "Peak Concurrence", "S/N Heuristic"
            var plot = CreatePlotFromData(datasetName, passed, edge, failed, "Passed", "Elution Edge", "Low Intensity", format);

            return plot;
        }

        /// <summary>
        /// Method to wrap calls to functions that require the single-thread apartment (generally all WinForms/WPF rendering classes require STA)
        /// </summary>
        /// <param name="action"></param>
        private static void GuaranteeSingleThreadApartment(Action action)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                action();
            }
            else
            {
                var thread = new Thread(() => action());
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }
        }

        private static void SavePlotToPdf(string filePath, PlotModel plot)
        {
            // OxyPlot.Core: Works, but is PDF; SVG should also work from core.
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var pdfExporter = new PdfExporter { Width = ExportImageWidth, Height = ExportImageHeight };
                pdfExporter.Export(plot, stream);
            }
        }

        private static void SavePlotToSvg(string filePath, PlotModel plot)
        {
            // OxyPlot.Core: Works, but is PDF; SVG should also work from core.
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var svgExporter = new SvgExporter { Width = ExportImageWidth, Height = ExportImageHeight };
                svgExporter.Export(plot, stream);
            }
        }

        private static void SavePlotToPng(string filePath, PlotModel plot)
        {
            // OxyPlot.WPF: requires STAThread, or new thread in STA
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                var pngExporter = new OxyPlot.Wpf.PngExporter() { Width = ExportImageWidth, Height = ExportImageHeight };
                pngExporter.Export(plot, stream);
            }
        }

        private static void SavePlotToJpg(string filePath, PlotModel plot)
        {
            // OxyPlot.WPF: requires STAThread, or new thread in STA

            var drawVisual = new DrawingVisual();
            using (var drawContext = drawVisual.RenderOpen())
            {
                var plotBitmap = OxyPlot.Wpf.PngExporter.ExportToBitmap(plot, ExportImageWidth, ExportImageHeight, OxyColors.White, 96);
                drawContext.DrawImage(plotBitmap, new Rect(0, 0, ExportImageWidth, ExportImageHeight));
            }

            var image = new RenderTargetBitmap(ExportImageWidth, ExportImageHeight, 96, 96, PixelFormats.Pbgra32);
            image.Render(drawVisual);
            image.Freeze();

            var jpg = new JpegBitmapEncoder();
            jpg.Frames.Add(BitmapFrame.Create(image));

            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                jpg.Save(stream);
            }
        }

        private static PlotModel CreatePlotFromData(string title, List<CompoundData> greenPoints, List<CompoundData> orangePoints, List<CompoundData> redPoints, string greenTitle, string orangeTitle, string redTitle, ExportFormat exFormat)
        {
            var plot = new OxyPlot.PlotModel()
            {
                TitlePadding = 0,
                Title = title,
                LegendPosition = LegendPosition.RightTop,
                LegendFontSize = 20,
                LegendBorder = OxyColors.Black,
                LegendBorderThickness = 1,
                //LegendItemAlignment = HorizontalAlignment.Right,
                LegendSymbolPlacement = LegendSymbolPlacement.Right,
            };

            var allPoints = redPoints.ToList();
            allPoints.AddRange(greenPoints);
            var xMax = allPoints.Max(x => x.ElutionTimeMidpoint);
            var xMin = allPoints.Min(x => x.ElutionTimeMidpoint);
            var yMax = allPoints.Max(x => x.TotalIntensitySum);
            var yMin = allPoints.Min(x => x.TotalIntensitySum);

            double seriesPointStrokeThickness = 0;
            double seriesPointMarkerSize = Math.Max(3, 7 - Math.Log10(allPoints.Count));

            var typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Medium);

            var font = plot.LegendFont;
            if (string.IsNullOrWhiteSpace(font))
            {
                font = plot.DefaultFont;
            }

            if (!string.IsNullOrWhiteSpace(font))
            {
                typeface = new Typeface(font);
            }

            //SetTextToEqualWidth(greenTitle, redTitle, out var greenTitleFmtted, out var redTitleFmtted, plot.LegendFontSize, false, true, typeface);
            //SetTextToEqualWidth($"({greenPoints.Count})", $"({redPoints.Count})", out var greenCountFmtted, out var redCountFmtted, plot.LegendFontSize, true, true, typeface);
            //var spaceGap = new string('\u00A0', 2);

            string greenFmtted;
            string orangeFmtted;
            string redFmtted;

            switch (exFormat)
            {
                case ExportFormat.PDF:
                case ExportFormat.SVG:
                    // These don't use Windows WPF rendering, so we can't determine text width correctly for the output.
                    goto default;
                case ExportFormat.PNG:
                    SetCombinedTextToEqualWidth(greenTitle, $"({greenPoints.Count})", orangeTitle, $"({orangePoints.Count})", redTitle, $"({redPoints.Count})", 2, out greenFmtted, out orangeFmtted, out redFmtted, plot.LegendFontSize, typeface);
                    break;
                default:
                    greenFmtted = $"{greenTitle} {"(" + greenPoints.Count + ")",6}";
                    orangeFmtted = $"{orangeTitle} {"(" + orangePoints.Count + ")",6}";
                    redFmtted = $"{redTitle} {"(" + redPoints.Count + ")",6}";
                    break;
            }

            var trackerFormatString = $"Compound: {{{nameof(CompoundData.CompoundName)}}}\nStart time: {{{nameof(CompoundData.StartTimeMinutes)}}}\nStop Time: {{{nameof(CompoundData.StopTimeMinutes)}}}\nTotal Abundance: {{{nameof(CompoundData.TotalIntensitySum)}}}\nMax Intensity: {{{nameof(CompoundData.MaxIntensity)}}}\nMax Intensity NET: {{{nameof(CompoundData.MaxIntensityNet)}}}";
            var pointMapper = new Func<object, ScatterPoint>(x => new ScatterPoint(((CompoundData) x).ElutionTimeMidpoint, ((CompoundData) x).TotalIntensitySum));

            var greenSeries = new ScatterSeries
            {
                Title = greenFmtted,
                MarkerType = MarkerType.Circle,
                MarkerStrokeThickness = seriesPointStrokeThickness,
                MarkerSize = seriesPointMarkerSize,
                MarkerFill = OxyColors.Green,
                ItemsSource = greenPoints,
                Mapping = pointMapper,
                TrackerFormatString = trackerFormatString,
            };

            var orangeSeries = new ScatterSeries
            {
                Title = orangeFmtted,
                MarkerType = MarkerType.Diamond,
                MarkerStrokeThickness = seriesPointStrokeThickness,
                MarkerSize = seriesPointMarkerSize,
                MarkerFill = OxyColors.Orange,
                ItemsSource = orangePoints,
                Mapping = pointMapper,
                TrackerFormatString = trackerFormatString,
            };

            var redSeries = new ScatterSeries
            {
                Title = redFmtted,
                MarkerType = MarkerType.Triangle,
                MarkerStrokeThickness = seriesPointStrokeThickness,
                MarkerSize = seriesPointMarkerSize,
                MarkerFill = OxyColors.Red,
                ItemsSource = redPoints,
                Mapping = pointMapper,
                TrackerFormatString = trackerFormatString,
            };

            var yAxisMax = yMax * 20; // seems big, but it is a logarithmic scale
            var yAxisMin = yMin / 10.0; // 1 step down on the log scale
            var yAxis = new LogarithmicAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity (Log10)",
                StringFormat = "#e00",
                FontSize = 20,
                Minimum = yAxisMin,
                AbsoluteMinimum = yAxisMin,
                Maximum = yAxisMax,
                AbsoluteMaximum = yAxisMax,

            };

            var xAxisMax = xMax * 1.03; // Add 3%
            var xAxisMin = xMin - xMax * 0.03; // Subtract 3% of the max
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (min)",
                FontSize = 20,
                Minimum = xAxisMin,
                AbsoluteMinimum = xAxisMin,
                Maximum = xAxisMax,
                AbsoluteMaximum = xAxisMax,
            };

            //var txtX = xMin;
            //var txtY = yMax + Math.Pow(10, Math.Log10(yMax));
            //var lenDiff = Math.Abs(greenTitle.Length - redTitle.Length) + 5;
            //var fmtString = $"{{0}} {{1,{lenDiff}}}\r\n{{2}} {{3,{lenDiff}}}";
            //var annotation = new TextAnnotation
            //{
            //    TextPosition = new DataPoint(txtX, txtY),
            //    //Text = $"{greenCountLegend}: {greenPoints.Count}\r\n{redCountLegend}: {redPoints.Count}",
            //    Text = string.Format(fmtString, greenTitle + ":", greenPoints.Count, redTitle + ":", redPoints.Count),
            //    TextHorizontalAlignment = HorizontalAlignment.Left,
            //    TextVerticalAlignment = VerticalAlignment.Bottom,
            //    FontSize = 25,
            //};

            plot.Axes.Add(yAxis);
            plot.Axes.Add(xAxis);
            plot.Series.Add(greenSeries);
            plot.Series.Add(orangeSeries);
            plot.Series.Add(redSeries);
            //plot.Annotations.Add(annotation);

            return plot;
        }

        #region Fancy text formatting

        private static void SetCombinedTextToEqualWidth(string in1P1, string in1P2, string in2P1, string in2P2, string in3P1, string in3P2, int gapSpaceCount, out string out1, out string out2, out string out3,
            double fontSize, Typeface typeface = null)
        {
            if (typeface == null)
            {
                typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Medium);
            }

            SetTextToEqualWidth(in1P1, in2P1, in3P1, out var in1P1Fmt, out var in2P1Fmt, out var in3P1Fmt, fontSize, false, false, typeface);
            SetTextToEqualWidth(in1P2, in2P2, in3P2, out var in1P2Fmt, out var in2P2Fmt, out var in3P2Fmt, fontSize, true, false, typeface);

            var gapSpaces = new string('\u00A0', Math.Max(gapSpaceCount, 0));

            var in1Fmtted = new FormattedText(in1P1Fmt + gapSpaces + in1P2Fmt, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var in2Fmtted = new FormattedText(in2P1Fmt + gapSpaces + in2P2Fmt, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var in3Fmtted = new FormattedText(in3P1Fmt + gapSpaces + in3P2Fmt, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var in1Width = in1Fmtted.Width;
            var in2Width = in2Fmtted.Width;
            var in3Width = in3Fmtted.Width;

            var calcIn1Width = in1Width;
            var calcIn2Width = in2Width;
            var calcIn3Width = in3Width;

            var spaceWidths = GetSpaceWidthsForFontSettings(fontSize, typeface);

            foreach (var spaceType in spaceWidths.OrderByDescending(x => x.Value))
            {
                if (Math.Max(calcIn2Width, calcIn3Width) - calcIn1Width >= spaceType.Value)
                {
                    in1P1Fmt += spaceType.Key;
                    calcIn1Width += spaceType.Value;
                }
                if (Math.Max(calcIn1Width, calcIn3Width) - calcIn2Width >= spaceType.Value)
                {
                    in2P1Fmt += spaceType.Key;
                    calcIn2Width += spaceType.Value;
                }
                if (Math.Max(calcIn1Width, calcIn2Width) - calcIn3Width >= spaceType.Value)
                {
                    in3P1Fmt += spaceType.Key;
                    calcIn3Width += spaceType.Value;
                }
            }

            var littleSpace = spaceWidths.Where(x => x.Value > 0.5).OrderBy(x => x.Value).First();
            var max12 = Math.Max(calcIn1Width, calcIn2Width);
            var max13 = Math.Max(calcIn1Width, calcIn3Width);
            var max23 = Math.Max(calcIn2Width, calcIn3Width);
            if (max23 - calcIn1Width > 0 && (calcIn1Width + littleSpace.Value) - max23 < max23 - calcIn1Width)
            {
                in1P1Fmt += littleSpace.Key;
                calcIn1Width += littleSpace.Value;
            }
            if (max13 - calcIn2Width > 0 && (calcIn2Width + littleSpace.Value) - max13 < max13 - calcIn2Width)
            {
                in2P1Fmt += littleSpace.Key;
                calcIn2Width += littleSpace.Value;
            }
            if (max12 - calcIn3Width > 0 && (calcIn3Width + littleSpace.Value) - max12 < max12 - calcIn3Width)
            {
                in3P1Fmt += littleSpace.Key;
                calcIn3Width += littleSpace.Value;
            }

            //Console.WriteLine("{0,8:F3} {1,8:F3}", in1Width, calcIn1Width);
            //Console.WriteLine("{0,8:F3} {1,8:F3}", in2Width, calcIn2Width);
            out1 = in1P1Fmt + gapSpaces + in1P2Fmt;
            out2 = in2P1Fmt + gapSpaces + in2P2Fmt;
            out3 = in3P1Fmt + gapSpaces + in3P2Fmt;
        }

        private static void SetTextToEqualWidth(string in1, string in2, string in3, out string out1, out string out2, out string out3, double fontSize, bool padFront = false, bool fillDiff = true, Typeface typeface = null)
        {
            if (typeface == null)
            {
                typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Medium);
            }

            out1 = in1;
            out2 = in2;
            out3 = in3;

            var in1Fmtted = new FormattedText(in1, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var in2Fmtted = new FormattedText(in2, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var in3Fmtted = new FormattedText(in3, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var in1Width = in1Fmtted.Width;
            var in2Width = in2Fmtted.Width;
            var in3Width = in2Fmtted.Width;
            var bigWidth = Math.Max(Math.Max(in1Width, in2Width), in3Width);
            var in1WidthDiff = bigWidth - in1Width;
            var in2WidthDiff = bigWidth - in2Width;
            var in3WidthDiff = bigWidth - in3Width;

            var spaceWidths = GetSpaceWidthsForFontSettings(fontSize, typeface);
            var spaceWidth = spaceWidths["\u00A0"];

            //var in1WidthAdd = (int)Math.Round(in1WidthDiff / spaceWidth);
            //var in2WidthAdd = (int)Math.Round(in2WidthDiff / spaceWidth);
            var in1WidthAdd = (int)(in1WidthDiff / spaceWidth); // Truncate decimals, to leave better gaps for filling
            var in2WidthAdd = (int)(in2WidthDiff / spaceWidth); // Truncate decimals, to leave better gaps for filling
            var in3WidthAdd = (int)(in3WidthDiff / spaceWidth); // Truncate decimals, to leave better gaps for filling

            var calcIn1Width = in1Width + in1WidthAdd * spaceWidth;
            var calcIn2Width = in2Width + in2WidthAdd * spaceWidth;
            var calcIn3Width = in3Width + in3WidthAdd * spaceWidth;
            //var calcIn1WidthOrig = calcIn1Width;
            //var calcIn2WidthOrig = calcIn2Width;

            var out1Spaces = new string('\u00A0', Math.Max(in1WidthAdd, 0));
            var out2Spaces = new string('\u00A0', Math.Max(in2WidthAdd, 0));
            var out3Spaces = new string('\u00A0', Math.Max(in3WidthAdd, 0));
            if (padFront)
            {
                out1 = out1Spaces + out1;
                out2 = out2Spaces + out2;
                out3 = out3Spaces + out3;
            }
            else
            {
                out1 += out1Spaces;
                out2 += out2Spaces;
                out3 += out3Spaces;
            }

            if (fillDiff)
            {
                foreach (var spaceType in spaceWidths.OrderByDescending(x => x.Value))
                {
                    if (Math.Max(calcIn2Width, calcIn3Width) - calcIn1Width >= spaceType.Value)
                    {
                        if (padFront)
                        {
                            out1 = spaceType.Key + out1;
                        }
                        else
                        {
                            out1 += spaceType.Key;
                        }

                        calcIn1Width += spaceType.Value;
                    }
                    if (Math.Max(calcIn1Width, calcIn3Width) - calcIn2Width >= spaceType.Value)
                    {
                        if (padFront)
                        {
                            out2 = spaceType.Key + out2;
                        }
                        else
                        {
                            out2 += spaceType.Key;
                        }

                        calcIn2Width += spaceType.Value;
                    }
                    if (Math.Max(calcIn1Width, calcIn2Width) - calcIn3Width >= spaceType.Value)
                    {
                        if (padFront)
                        {
                            out3 = spaceType.Key + out3;
                        }
                        else
                        {
                            out3 += spaceType.Key;
                        }

                        calcIn3Width += spaceType.Value;
                    }
                }
            }

            //Console.WriteLine("{0,8:F3} {1,8:F3} {2,8:F3} {3,8:F3} {4,8:F3} {5,8:F3}", in1Width, bigWidth, in1WidthDiff, in1WidthAdd, calcIn1WidthOrig, calcIn1Width);
            //Console.WriteLine("{0,8:F3} {1,8:F3} {2,8:F3} {3,8:F3} {4,8:F3} {5,8:F3}", in2Width, bigWidth, in2WidthDiff, in2WidthAdd, calcIn2WidthOrig, calcIn2Width);
        }

        /// <summary>
        /// Get the formatted widths of several types of spaces for filling gaps on variable-width fonts
        /// </summary>
        /// <param name="fontSize"></param>
        /// <param name="typeface"></param>
        /// <returns></returns>
        private static Dictionary<string, double> GetSpaceWidthsForFontSettings(double fontSize, Typeface typeface)
        {
            const string spaceBookend = "e";
            var spaceBookendFmtted = new FormattedText(spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            //var spaceFmtted = new FormattedText(spaceBookend + " " + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black); // normal space by itself outputs inaccurate width of 0
            var spaceFmtted = new FormattedText("\u00A0", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black); // provides accurate width
            var thinSpaceFmtted = new FormattedText(spaceBookend + "\u2009" + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var hairSpaceFmtted = new FormattedText(spaceBookend + "\u200A" + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            //var thinSpaceFmtted = new FormattedText("\u2009", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black); // outputs inaccurate width of 0
            //var hairSpaceFmtted = new FormattedText("\u200A", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black); // does provide accurate width
            var emDot33SpaceFmtted = new FormattedText(spaceBookend + "\u2004" + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var emDot25SpaceFmtted = new FormattedText(spaceBookend + "\u2005" + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var emDot1666SpaceFmtted = new FormattedText(spaceBookend + "\u2006" + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var punctSpaceFmtted = new FormattedText(spaceBookend + "\u2008" + spaceBookend, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
            var spaceBookendWidth = spaceBookendFmtted.Width;
            //var spaceWidth = spaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;
            var spaceWidth = spaceFmtted.Width;
            var thinSpaceWidth = thinSpaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;
            var hairSpaceWidth = hairSpaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;
            //var thinSpaceWidth = thinSpaceFmtted.Width;
            //var hairSpaceWidth = hairSpaceFmtted.Width;
            var emDot33SpaceWidth = emDot33SpaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;
            var emDot25SpaceWidth = emDot25SpaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;
            var emDot1666SpaceWidth = emDot1666SpaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;
            var punctSpaceWidth = punctSpaceFmtted.Width - spaceBookendWidth - spaceBookendWidth;

            //Console.WriteLine("{0,8:F3} {1,8:F3} {2,8:F3} {3,8:F3} {4,8:F3} {5,8:F3} {6,8:F3}", spaceWidth, thinSpaceWidth, hairSpaceWidth, emDot33SpaceWidth, emDot25SpaceWidth, emDot1666SpaceWidth, punctSpaceWidth);

            var spaceWidths = new Dictionary<string, double>()
            {
                {"\u00A0", spaceWidth},
                {"\u2004", emDot33SpaceWidth},
                {"\u2005", emDot25SpaceWidth},
                {"\u2006", emDot1666SpaceWidth},
                {"\u2008", punctSpaceWidth},
                {"\u2009", thinSpaceWidth},
                {"\u200A", hairSpaceWidth},
            };

            return spaceWidths;
        }

        #endregion
    }
}
