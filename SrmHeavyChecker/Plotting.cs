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

        public static BitmapSource ConvertToBitmapImage(PlotModel plot, int width, int height, int resolution = 96)
        {
            var scale = resolution / 96.0;
            return OxyPlot.Wpf.PngExporter.ExportToBitmap(plot, (int) (width * scale), (int) (height * scale), OxyColors.White, resolution);
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
                LegendItemAlignment = OxyPlot.HorizontalAlignment.Right,
                LegendSymbolPlacement = LegendSymbolPlacement.Right,
            };

            var xMax = result.Transitions.Max(x => x.StopTimeMinutes);
            var xMin = result.Transitions.Min(x => x.StartTimeMinutes); ;
            var yMax = result.Transitions.Max(x => x.MaxIntensity);

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity",
                StringFormat = "#.#e00",
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

        public static void PlotTransition(TransitionData transition, string filepath, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return;
            }

            var plot = CreateTransitionPlot(transition, format);

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

        public static PlotModel CreateTransitionPlot(TransitionData transition, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return null;
            }

            var plot = new OxyPlot.PlotModel()
            {
                TitlePadding = 0,
                Title = transition.CompoundName,
                LegendPosition = LegendPosition.RightTop,
                LegendFontSize = 20,
                LegendBorder = OxyColors.Black,
                LegendBorderThickness = 1,
                //LegendItemAlignment = HorizontalAlignment.Right,
                LegendSymbolPlacement = LegendSymbolPlacement.Right,
            };

            var xMax = transition.StopTimeMinutes;
            var xMin = transition.StartTimeMinutes;
            var yMax = transition.MaxIntensity;

            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity",
                StringFormat = "#.#e00",
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

            var series = new LineSeries()
            {
                Title = $"{transition.ProductMz:F2}",
                MarkerType = MarkerType.None,
                ItemsSource = transition.Intensities,
                Mapping = pointMapper,
                TrackerFormatString = trackerFormatString,
            };

            plot.Series.Add(series);

            return plot;
        }

        public static PlotModel CreatePlot(List<CompoundData> results, string datasetName, ExportFormat format = ExportFormat.PNG)
        {
            if (format == ExportFormat.NoImageExport)
            {
                return null;
            }

            var passed = new SeriesData(results.Where(x => x.PassesIntensity && x.PassesNET).ToList(), "Passed", OxyColors.Green, MarkerType.Circle);
            var edge = new SeriesData(results.Where(x => x.PassesIntensity && !x.PassesNET).ToList(), "Elution Edge", OxyColors.Orange, MarkerType.Diamond);
            var failed = new SeriesData(results.Where(x => !x.PassesIntensity).ToList(), "Low Intensity", OxyColors.Red, MarkerType.Triangle);

            var items = new List<SeriesData>() {passed, edge, failed};

            // TODO: Add "Peak Concurrence", "S/N Heuristic"
            var plot = CreatePlotFromData(datasetName, items, format);

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

        private class SeriesData
        {
            public List<CompoundData> Points { get; }
            public string Title { get; }
            public TitleSpaceFormattingCombined TitleFormatted { get; }
            public MarkerType PointMarker { get; }
            public OxyColor PointColor { get; }

            public SeriesData(List<CompoundData> points, string title, OxyColor pointColor, MarkerType pointMarker)
            {
                Points = points;
                Title = title;
                TitleFormatted = new TitleSpaceFormattingCombined(title, $"({points.Count})");
                PointColor = pointColor;
                PointMarker = pointMarker;
            }
        }

        private static PlotModel CreatePlotFromData(string title, List<SeriesData> data, ExportFormat exFormat)
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

            var allPoints = data.SelectMany(x => x.Points).ToList();
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

            switch (exFormat)
            {
                case ExportFormat.PDF:
                case ExportFormat.SVG:
                    // These don't use Windows WPF rendering, so we can't determine text width correctly for the output.
                    goto default;
                case ExportFormat.PNG:
                case ExportFormat.JPEG:
                    SetCombinedTextToEqualWidth(data.Select(x => x.TitleFormatted).ToList(), 2, plot.LegendFontSize, typeface);
                    break;
                default:
                    foreach (var item in data)
                    {
                        item.TitleFormatted.TitleFormatted = $"{item.Title} {"(" + item.Points.Count + ")",6}";
                    }
                    break;
            }

            var trackerFormatString = $"Compound: {{{nameof(CompoundData.CompoundName)}}}\nStart time: {{{nameof(CompoundData.StartTimeMinutes)}}}\nStop Time: {{{nameof(CompoundData.StopTimeMinutes)}}}\nTotal Abundance: {{{nameof(CompoundData.TotalIntensitySum)}}}\nMax Intensity: {{{nameof(CompoundData.MaxIntensity)}}}\nMax Intensity NET: {{{nameof(CompoundData.MaxIntensityNet)}}}";
            var pointMapper = new Func<object, ScatterPoint>(x => new ScatterPoint(((CompoundData)x).ElutionTimeMidpoint, ((CompoundData)x).TotalIntensitySum));

            var yAxisMax = yMax * 20; // seems big, but it is a logarithmic scale
            var yAxisMin = yMin / 10.0; // 1 step down on the log scale
            var yAxis = new LogarithmicAxis
            {
                Position = AxisPosition.Left,
                Title = "Intensity (Log10)",
                StringFormat = "#.#e00",
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

            plot.Axes.Add(yAxis);
            plot.Axes.Add(xAxis);

            foreach (var item in data)
            {
                var series = new ScatterSeries
                {
                    Title = item.TitleFormatted.TitleFormatted,
                    MarkerType = item.PointMarker,
                    MarkerStrokeThickness = seriesPointStrokeThickness,
                    MarkerSize = seriesPointMarkerSize,
                    MarkerFill = item.PointColor,
                    ItemsSource = item.Points,
                    Mapping = pointMapper,
                    TrackerFormatString = trackerFormatString,
                };

                plot.Series.Add(series);
            }

            return plot;
        }

        #region Fancy text formatting

        private class TitleSpaceFormattingCombined
        {
            public TitleSpaceFormmatting TitlePart1 { get; }
            public TitleSpaceFormmatting TitlePart2 { get; }
            public string TitleFormatted { get; set; }
            public double CalculatedFinalWidth { get; set; }
            public double MaxCalculatedFinalWidthOthers { get; set; }

            public TitleSpaceFormattingCombined(string titlePart1, string titlePart2)
            {
                TitlePart1 = new TitleSpaceFormmatting(titlePart1);
                TitlePart2 = new TitleSpaceFormmatting(titlePart2);
            }
        }

        private static void SetCombinedTextToEqualWidth(List<TitleSpaceFormattingCombined> textItems, int gapSpaceCount, double fontSize, Typeface typeface = null)
        {
            if (typeface == null)
            {
                typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Medium);
            }

            SetTextToEqualWidth(textItems.Select(x => x.TitlePart1).ToList(), fontSize, false, false, typeface);
            SetTextToEqualWidth(textItems.Select(x => x.TitlePart2).ToList(), fontSize, true, false, typeface);

            var gapSpaces = new string('\u00A0', Math.Max(gapSpaceCount, 0));

            foreach (var item in textItems)
            {
                var fmtted = new FormattedText(item.TitlePart1.TitleFormatted + gapSpaces + item.TitlePart2.TitleFormatted, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
                item.CalculatedFinalWidth = fmtted.Width;
            }

            var spaceWidths = GetSpaceWidthsForFontSettings(fontSize, typeface);

            foreach (var spaceType in spaceWidths.OrderByDescending(x => x.Value))
            {
                var maxCalcWidth = textItems.Max(x => x.CalculatedFinalWidth);
                foreach (var item in textItems)
                {
                    if (maxCalcWidth - item.CalculatedFinalWidth >= spaceType.Value)
                    {
                        item.TitlePart1.TitleFormatted += spaceType.Key;
                        item.CalculatedFinalWidth += spaceType.Value;
                    }
                }
            }

            var littleSpace = spaceWidths.Where(x => x.Value > 0.5).OrderBy(x => x.Value).First();
            foreach (var item in textItems)
            {
                item.MaxCalculatedFinalWidthOthers = textItems.Where(x => x != item).Max(x => x.CalculatedFinalWidth);
            }

            foreach (var item in textItems)
            {
                if (item.MaxCalculatedFinalWidthOthers - item.CalculatedFinalWidth > 0 &&
                    (item.CalculatedFinalWidth + littleSpace.Value) - item.MaxCalculatedFinalWidthOthers <
                    item.MaxCalculatedFinalWidthOthers - item.CalculatedFinalWidth)
                {
                    item.TitlePart1.TitleFormatted += littleSpace.Key;
                    item.CalculatedFinalWidth += littleSpace.Value;
                    item.TitleFormatted = item.TitlePart1.TitleFormatted + gapSpaces + item.TitlePart2.TitleFormatted;
                }
                else
                {
                    item.TitleFormatted = item.TitlePart1.TitleFormatted + gapSpaces + item.TitlePart2.TitleFormatted;
                }
            }
        }

        private class TitleSpaceFormmatting
        {
            public string Title { get; }
            public string TitleFormatted { get; set; }
            public double UngappedWidth { get; set; }
            public double CalculatedFinalWidth { get; set; }

            public TitleSpaceFormmatting(string title)
            {
                Title = title;
                TitleFormatted = title;
            }
        }

        private static void SetTextToEqualWidth(List<TitleSpaceFormmatting> textItems, double fontSize, bool padFront = false, bool fillDiff = true, Typeface typeface = null)
        {
            if (typeface == null)
            {
                typeface = new Typeface(SystemFonts.MessageFontFamily, SystemFonts.MessageFontStyle, SystemFonts.MessageFontWeight, FontStretches.Medium);
            }

            foreach (var item in textItems)
            {
                var fmtted = new FormattedText(item.Title, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, fontSize * (96.0 / 72.0), Brushes.Black);
                item.UngappedWidth = fmtted.Width;
            }

            var bigWidth = textItems.Max(x => x.UngappedWidth);

            var spaceWidths = GetSpaceWidthsForFontSettings(fontSize, typeface);
            var spaceWidth = spaceWidths["\u00A0"];

            foreach (var item in textItems)
            {
                var widthDiff = bigWidth - item.UngappedWidth;
                var widthAdd = (int) (widthDiff / spaceWidth); // Truncate decimals, to leave better gaps for filling
                item.CalculatedFinalWidth = item.UngappedWidth + widthAdd * spaceWidth;

                var spaces = new string('\u00A0', Math.Max(widthAdd, 0));

                if (padFront)
                {
                    item.TitleFormatted = spaces + item.TitleFormatted;
                }
                else
                {
                    item.TitleFormatted += spaces;
                }
            }

            if (fillDiff)
            {
                foreach (var spaceType in spaceWidths.OrderByDescending(x => x.Value))
                {
                    var maxCalcWidth = textItems.Max(x => x.CalculatedFinalWidth);
                    foreach (var item in textItems)
                    {
                        if (maxCalcWidth - item.CalculatedFinalWidth >= spaceType.Value)
                        {
                            if (padFront)
                            {
                                item.TitleFormatted = spaceType.Key + item.TitleFormatted;
                            }
                            else
                            {
                                item.TitleFormatted += spaceType.Key;
                            }

                            item.CalculatedFinalWidth += spaceType.Value;
                        }
                    }
                }
            }
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
