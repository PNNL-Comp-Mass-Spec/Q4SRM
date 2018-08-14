using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Q4SRM.Data;
using Q4SRM.Settings;

namespace Q4SRM.Output
{
    public class PdfWriter
    {
        // by default PDFSharp uses "point" as the unit, for reference: 72pts = 1 inch as defined by the DTP(or PostScript) point.
        // Originally, this margin was set to an odd 25pt or 0.375 inch, "standard" margins are usually 0.5 or 1 inch, aka,
        // 36 or 72 pt, so this was changed to 36pt. The library could be enhanced by moving the margin to the document
        // model and letting the user control the margins through it, instead of using a constant
        private const double PageMargin = 36;
        private const double DoublePageMargin = PageMargin * 2;

        private PdfPage currentPage;
        private XGraphics currentPageGraphics;
        private readonly PdfDocument document;
        private readonly XFont fontHeader;
        private readonly XFont fontHeader2;
        private readonly XFont fontDefault;
        private readonly FontFamily fontFamily;

        private double currentPageY;

        public string DatasetName { get; }
        public string DatasetPath { get; }
        public string SavePath { get; }

        public PdfWriter(string datasetName, string datasetPath, string savePath)
        {
            DatasetName = datasetName;
            DatasetPath = datasetPath;
            SavePath = savePath;

            fontFamily = new FontFamily("Arial");
            fontHeader = new XFont(fontFamily, 20, XFontStyle.Regular);
            fontHeader2 = new XFont(fontFamily, 16, XFontStyle.Regular);
            fontDefault = new XFont(fontFamily, 10, XFontStyle.Regular);
            document = new PdfDocument();
            AddPage();
        }

        public void WritePdf(List<CompoundData> results, ISettingsData settings, bool useNonVectorImages = false)
        {
            if (useNonVectorImages && Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                var thread = new Thread(() => WritePdf(results, settings, useNonVectorImages));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                return;
            }

            Func<List<CompoundData>, int, int, int, XImage> summaryPlotCreateMethod = CreatePlot;
            Func<CompoundData, int, int, int, XImage> plotCreateMethod = CreatePlot;
            var plotSizeMultiplier = 3d;
            if (useNonVectorImages)
            {
                summaryPlotCreateMethod = CreatePlotBitmapWpf;
                plotCreateMethod = CreatePlotBitmapWpf;
                // Plot text is naturally larger in OxyPlot.WPF-rendered images, so increase the size multiplier
                plotSizeMultiplier = 4;
            }

            // Guarantee that the plots will be backed at 300+ DPI
            var desiredResolution = 300;
            var resolution = 96;

            // Add a header: Dataset name
            currentPageY += AddText(DatasetName, fontHeader, 0.75, position: XStringFormats.BaseLineCenter);

            // Add the analysis settings
            currentPageY += AddText("Q4SRM Analysis settings:", fontHeader2);
            currentPageY += AddText($"Input file path: {DatasetPath}", fontDefault);
            currentPageY += AddText($"Compound minimum summed intensity threshold: {settings.DefaultIntensityThreshold:F0}", fontDefault);
            currentPageY += AddText($"Compound peak elution concurrence threshold (minutes): {settings.ElutionConcurrenceThresholdMinutes:F2}", fontDefault);
            currentPageY += AddText($"Transition minimum peak distance from elution window edge (minutes): {settings.ElutionConcurrenceThresholdMinutes:F2}", fontDefault);
            currentPageY += AddText($"Transition minimum signal to noise heuristic (max intensity / median intensity): {settings.SignalToNoiseHeuristicThreshold:F2}", fontDefault);
            if (!string.IsNullOrWhiteSpace(settings.CompoundThresholdFilePath))
            {
                currentPageY += AddText($"Per-compound intensity threshold file path: {settings.CompoundThresholdFilePath}", fontDefault);
                currentPageY += AddText($"Per-compound intensity threshold file SHA1: {settings.CompoundThresholdFileSha1Hash}", fontDefault);
            }

            // Add the summary plot
            var summaryPlotAspectRatio = 4.0 / 3.0;
            var summaryPlotWidth = currentPage.Width - DoublePageMargin;
            var summaryPlotHeight = summaryPlotWidth / summaryPlotAspectRatio;
            var summaryPlotWidthWpf = (int)(summaryPlotWidth * plotSizeMultiplier);
            var summaryPlotHeightWpf = (int)(summaryPlotHeight * plotSizeMultiplier);
            var summaryPlotImage = summaryPlotCreateMethod(results, summaryPlotWidthWpf, summaryPlotHeightWpf, resolution);
            AddPlot(summaryPlotImage, PageMargin, summaryPlotWidth, summaryPlotHeight, null);
            currentPageY += summaryPlotHeight + 25;

            AddPage();

            // Add the error plots
            currentPageY += AddText("Errors:", fontHeader2);
            currentPageY += AddText("Key: int=intensity, pp=peak position, ec=elution concurrence, sn=S/N heuristic", fontDefault);

            var plotSize = (currentPage.Width - DoublePageMargin - 10) / 3;
            var wpfRenderSize = (int)(plotSize * plotSizeMultiplier);

            // Check the resolution vs. pdf render size
            if (wpfRenderSize < desiredResolution / (double) resolution * plotSize)
            {
                wpfRenderSize = (int) (desiredResolution / (double) resolution * plotSize);
            }

            var counter = 0;
            var xOffset = PageMargin;
            var textHeight = GetTextHeight("int", currentPageGraphics, fontDefault);
            foreach (var result in results.Where(x => !x.PassesAllThresholds).OrderBy(x => x.CompoundName))
            {
                if (counter % 3 == 0 && counter > 0)
                {
                    xOffset = PageMargin;
                    currentPageY += textHeight + plotSize + 25;
                    if (currentPageY + textHeight + plotSize > currentPage.Height - PageMargin)
                    {
                        AddPage();
                    }
                }

                counter++;

                var plotImage = plotCreateMethod(result, wpfRenderSize, wpfRenderSize, resolution);
                AddPlot(plotImage, xOffset, plotSize, plotSize, $"{counter}: {GetCompoundDataHeader(result)}");
                xOffset += plotSize + 5;
            }

            currentPageY += textHeight + plotSize + 25;
            if (currentPageY + textHeight + plotSize > currentPage.Height - PageMargin)
            {
                AddPage();
            }

            // Add the passing plots
            currentPageY += AddText("Passes:", fontHeader2);

            counter = 0;
            xOffset = PageMargin;
            foreach (var result in results.Where(x => x.PassesAllThresholds).OrderBy(x => x.CompoundName))
            {
                if (counter % 3 == 0 && counter > 0)
                {
                    xOffset = PageMargin;
                    currentPageY += plotSize;
                    if (currentPageY + plotSize > currentPage.Height - PageMargin)
                    {
                        AddPage();
                    }
                }
                counter++;

                var plotImage = plotCreateMethod(result, wpfRenderSize, wpfRenderSize, resolution);
                AddPlot(plotImage, xOffset, plotSize, plotSize, null);
                xOffset += plotSize;
            }

            currentPageY += plotSize + 25;
            //if (currentPageY + plotSize > currentPage.Height - PageMargin)
            //{
            //    AddPage();
            //}

            document.Save(SavePath);
        }

        private string GetCompoundDataHeader(CompoundData result)
        {
            var items = new List<string>();
            if (!result.PassesIntensity)
            {
                items.Add("int");
            }
            if (!result.PassesNET)
            {
                items.Add("pp");
            }
            if (!result.PassesElutionConcurrence)
            {
                items.Add("ec");
            }
            if (!result.PassesSignalToNoiseHeuristic)
            {
                items.Add("sn");
            }

            return string.Join(", ", items);
        }

        private double AddPlot(XImage plotImage, double xOffset, double width, double height, string header = null)
        {
            var yPosition = currentPageY;
            if (!string.IsNullOrWhiteSpace(header))
            {
                yPosition += AddText(header, fontDefault, x: xOffset) - 10;
            }

            currentPageGraphics.DrawImage(plotImage, new XRect(xOffset, yPosition, width, height));
            plotImage.Dispose();

            return yPosition - currentPageY + height;
        }

        /// <summary>
        /// Create the plot, and convert it to an XImage. Remember to dispose of the returned object, and that this MUST be run with threading ApartmentState.STA
        /// </summary>
        /// <param name="result"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private XImage CreatePlot(CompoundData result, int width, int height, int resolution = 96)
        {
            var plot = Plotting.CreateCompoundPlot(result, Plotting.ExportFormat.PDF);
            using (var memStream = new MisbehavingMemoryStream())
            {
                memStream.PreventDispose = true;
                Plotting.SavePlotToPdf(memStream, plot, width, height);
                memStream.Seek(0, SeekOrigin.Begin);
                var pdfForm = XPdfForm.FromStream(memStream);
                memStream.PreventDispose = false;
                return pdfForm;
            }
        }

        /// <summary>
        /// Create the plot, and convert it to an XImage. Remember to dispose of the returned object, and that this MUST be run with threading ApartmentState.STA
        /// </summary>
        /// <param name="results"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private XImage CreatePlot(List<CompoundData> results, int width, int height, int resolution = 96)
        {
            var plot = Plotting.CreatePlot(results, DatasetName, Plotting.ExportFormat.PDF);
            using (var memStream = new MisbehavingMemoryStream())
            {
                memStream.PreventDispose = true;
                Plotting.SavePlotToPdf(memStream, plot, width, height);
                memStream.Seek(0, SeekOrigin.Begin);
                var pdfForm = XPdfForm.FromStream(memStream);
                memStream.PreventDispose = false;
                return pdfForm;
            }
        }

        /// <summary>
        /// OxyPlot improperly closes the passed-in stream on the PdfExport. This class gets around it by disallowing the dispose based on a flag.
        /// </summary>
        private class MisbehavingMemoryStream : MemoryStream
        {
            public bool PreventDispose { get; set; }

            public override void Close()
            {
                if (!PreventDispose)
                {
                    base.Close();
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (!PreventDispose)
                {
                    base.Dispose(disposing);
                }
            }
        }

        /// <summary>
        /// Create the plot, and convert it to an XImage. Remember to dispose of the returned object, and that this MUST be run with threading ApartmentState.STA
        /// </summary>
        /// <param name="result"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private XImage CreatePlotBitmapWpf(CompoundData result, int width, int height, int resolution = 96)
        {
            var plot = Plotting.CreateCompoundPlot(result);
            var bitmap = Plotting.ConvertToBitmapImage(plot, width, height, resolution);
            bitmap.Freeze();
            var image = XImage.FromBitmapSource(bitmap);
            return image;
        }

        /// <summary>
        /// Create the plot, and convert it to an XImage. Remember to dispose of the returned object, and that this MUST be run with threading ApartmentState.STA
        /// </summary>
        /// <param name="results"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        private XImage CreatePlotBitmapWpf(List<CompoundData> results, int width, int height, int resolution = 96)
        {
            var plot = Plotting.CreatePlot(results, DatasetName);
            var bitmap = Plotting.ConvertToBitmapImage(plot, width, height, resolution);
            bitmap.Freeze();
            var image = XImage.FromBitmapSource(bitmap);
            return image;
        }

        private void AddPage()
        {
            currentPage = document.AddPage();
            //page.Size = ;
            currentPageGraphics = XGraphics.FromPdfPage(currentPage);
            currentPageY = PageMargin;
        }

        private double AddText(string text, XFont font, double yOffset = 0, double x = 0, double width = -1, XStringFormat position = null)
        {
            var textHeight = GetTextHeight(text, currentPageGraphics, font);
            var y = currentPageY;

            if (!yOffset.Equals(0))
            {
                y += textHeight * yOffset;
            }

            if (x < PageMargin || x > currentPage.Width - PageMargin)
            {
                x = PageMargin;
            }

            if (width < x || width > currentPage.Width - DoublePageMargin)
            {
                width = currentPage.Width - DoublePageMargin;
            }

            if (position == null)
            {
                position = XStringFormats.BaseLineLeft;
            }

            var height = textHeight;
            if (position.LineAlignment == XLineAlignment.BaseLine)
            {
                height = 0;
            }

            currentPageGraphics.DrawString(text, font, XBrushes.Black, new XRect(x, y, width, height));

            return y - currentPageY + textHeight;
        }

        private static double GetTextHeight(string text, XGraphics gfx, XFont font)
        {
            var textSize = gfx.MeasureString(text, font);
            return textSize.Height;
        }
    }
}
