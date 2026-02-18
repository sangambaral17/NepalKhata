using ClosedXML.Excel;
using HardwareShopPro.Core.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Serilog;

namespace HardwareShopPro.Core.Services;

/// <summary>
/// Export service: generates PDF and Excel reports for sales data.
/// </summary>
public static class ReportExportService
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ReportExportService));

    private static readonly XColor Primary = XColor.FromArgb(79, 70, 229);
    private static readonly XColor HeaderBg = XColor.FromArgb(243, 244, 246);
    private static readonly XColor TextDark = XColor.FromArgb(33, 37, 41);
    private static readonly XColor TextLight = XColor.FromArgb(107, 114, 128);

    // ═══════════════════════════════════════════════════════════════════════
    // PDF REPORT
    // ═══════════════════════════════════════════════════════════════════════

    public static string ExportSalesReportPdf(
        string title,
        DateTime startDate, DateTime endDate,
        decimal totalRevenue, int totalInvoices, decimal avgOrderValue,
        IEnumerable<Invoice> invoices,
        IEnumerable<ProductSalesReport> topProducts,
        string businessName,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Report_{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(outputDirectory, fileName);

        var doc = new PdfDocument();
        doc.Info.Title = $"{title} Report";
        var page = doc.AddPage();
        page.Width = XUnit.FromMillimeter(210);
        page.Height = XUnit.FromMillimeter(297);
        var gfx = XGraphics.FromPdfPage(page);

        double y = 40, left = 40, right = page.Width.Point - 40;
        double width = right - left;

        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        var headerFont = new XFont("Arial", 10, XFontStyle.Bold);
        var normalFont = new XFont("Arial", 9);
        var smallFont = new XFont("Arial", 8);

        // ─── Title ───────────────────────────────────────────────────
        gfx.DrawString(businessName, new XFont("Arial", 12, XFontStyle.Bold),
            new XSolidBrush(TextLight), left, y);
        y += 22;
        gfx.DrawString($"{title} Report", titleFont, new XSolidBrush(Primary), left, y);
        y += 24;
        gfx.DrawString($"{startDate:dd MMM yyyy} — {endDate:dd MMM yyyy}", normalFont,
            new XSolidBrush(TextLight), left, y);
        y += 8;
        gfx.DrawLine(new XPen(Primary, 1.5), left, y, right, y);
        y += 20;

        // ─── Summary Cards ──────────────────────────────────────────
        double cardW = (width - 20) / 3;
        DrawSummaryCard(gfx, left, y, cardW, "Total Revenue", $"Rs.{totalRevenue:N2}");
        DrawSummaryCard(gfx, left + cardW + 10, y, cardW, "Total Invoices", totalInvoices.ToString());
        DrawSummaryCard(gfx, left + 2 * (cardW + 10), y, cardW, "Avg Order Value", $"Rs.{avgOrderValue:N2}");
        y += 58;

        // ─── Invoice Table ──────────────────────────────────────────
        gfx.DrawString("Invoice Details", headerFont, new XSolidBrush(Primary), left, y);
        y += 18;

        // Header row
        gfx.DrawRectangle(new XSolidBrush(Primary), left, y, width, 20);
        string[] cols = { "Invoice #", "Date", "Customer", "Amount", "Status" };
        double[] colW = { 100, 80, width - 100 - 80 - 80 - 60, 80, 60 };
        double xPos = left;
        foreach (var (col, i) in cols.Select((v, i) => (v, i)))
        {
            gfx.DrawString(col, new XFont("Arial", 8, XFontStyle.Bold), XBrushes.White, xPos + 4, y + 13);
            xPos += colW[i];
        }
        y += 22;

        int rowIdx = 0;
        foreach (var inv in invoices.Take(40)) // Limit rows per page
        {
            if (y > page.Height.Point - 80)
            {
                page = doc.AddPage();
                gfx = XGraphics.FromPdfPage(page);
                y = 40;
            }

            if (rowIdx % 2 == 1)
                gfx.DrawRectangle(new XSolidBrush(HeaderBg), left, y - 3, width, 16);

            xPos = left;
            string[] row = {
                inv.InvoiceNumber,
                inv.Date.ToString("dd/MM/yyyy"),
                inv.CustomerName ?? "Walk-in",
                $"Rs.{inv.TotalAmount:N2}",
                inv.PaymentStatus.ToString()
            };
            foreach (var (val, i) in row.Select((v, i) => (v, i)))
            {
                gfx.DrawString(val, normalFont, new XSolidBrush(TextDark), xPos + 4, y + 9);
                xPos += colW[i];
            }
            y += 18;
            rowIdx++;
        }
        y += 16;

        // ─── Top Products ───────────────────────────────────────────
        if (topProducts.Any() && y < page.Height.Point - 120)
        {
            gfx.DrawString("Top Selling Products", headerFont, new XSolidBrush(Primary), left, y);
            y += 18;

            foreach (var p in topProducts.Take(5))
            {
                gfx.DrawString($"• {p.ProductName}", normalFont, new XSolidBrush(TextDark), left + 8, y + 10);
                gfx.DrawString($"Qty: {p.TotalQuantity}  |  Rev: Rs.{p.TotalRevenue:N2}", smallFont,
                    new XSolidBrush(TextLight), left + 200, y + 10);
                y += 16;
            }
        }

        // ─── Footer ─────────────────────────────────────────────────
        var footerY = page.Height.Point - 40;
        gfx.DrawString($"Generated on {DateTime.Now:dd MMM yyyy HH:mm} · {businessName}",
            smallFont, new XSolidBrush(TextLight), page.Width.Point / 2, footerY, XStringFormats.TopCenter);

        doc.Save(filePath);
        Logger.Information("PDF report exported: {Path}", filePath);
        return filePath;
    }

    private static void DrawSummaryCard(XGraphics gfx, double x, double y, double w, string label, string value)
    {
        gfx.DrawRectangle(new XSolidBrush(HeaderBg), x, y, w, 46);
        gfx.DrawString(label, new XFont("Arial", 8), new XSolidBrush(TextLight), x + 10, y + 16);
        gfx.DrawString(value, new XFont("Arial", 14, XFontStyle.Bold), new XSolidBrush(Primary), x + 10, y + 38);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EXCEL REPORT (ClosedXML)
    // ═══════════════════════════════════════════════════════════════════════

    public static string ExportSalesReportExcel(
        string title,
        DateTime startDate, DateTime endDate,
        decimal totalRevenue, int totalInvoices, decimal avgOrderValue,
        IEnumerable<Invoice> invoices,
        IEnumerable<ProductSalesReport> topProducts,
        string businessName,
        string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Report_{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        var filePath = Path.Combine(outputDirectory, fileName);

        using var wb = new XLWorkbook();

        // ─── Sales Sheet ────────────────────────────────────────────
        var ws = wb.AddWorksheet("Sales Report");

        // Title
        ws.Cell("A1").Value = $"{businessName} - {title} Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:E1").Merge();

        ws.Cell("A2").Value = $"{startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}";
        ws.Range("A2:E2").Merge();

        // Summary row
        ws.Cell("A4").Value = "Total Revenue";
        ws.Cell("B4").Value = totalRevenue;
        ws.Cell("B4").Style.NumberFormat.Format = "#,##0.00";
        ws.Cell("C4").Value = "Total Invoices";
        ws.Cell("D4").Value = totalInvoices;
        ws.Cell("E4").Value = "Avg Order Value";
        ws.Cell("F4").Value = avgOrderValue;
        ws.Cell("F4").Style.NumberFormat.Format = "#,##0.00";
        ws.Row(4).Style.Font.Bold = true;

        // Header
        int row = 6;
        string[] headers = { "Invoice #", "Date", "Customer", "Payment Mode", "Status", "Amount" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
        }
        var headerRange = ws.Range(row, 1, row, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(79, 70, 229);
        headerRange.Style.Font.FontColor = XLColor.White;

        // Data rows
        row++;
        foreach (var inv in invoices)
        {
            ws.Cell(row, 1).Value = inv.InvoiceNumber;
            ws.Cell(row, 2).Value = inv.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = inv.CustomerName ?? "Walk-in";
            ws.Cell(row, 4).Value = inv.PaymentMode.ToString();
            ws.Cell(row, 5).Value = inv.PaymentStatus.ToString();
            ws.Cell(row, 6).Value = inv.TotalAmount;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();

        // ─── Top Products Sheet ─────────────────────────────────────
        var ws2 = wb.AddWorksheet("Top Products");
        ws2.Cell("A1").Value = "Top Selling Products";
        ws2.Cell("A1").Style.Font.Bold = true;
        ws2.Cell("A1").Style.Font.FontSize = 14;

        ws2.Cell("A3").Value = "Product";
        ws2.Cell("B3").Value = "Total Qty Sold";
        ws2.Cell("C3").Value = "Total Revenue";
        ws2.Range("A3:C3").Style.Font.Bold = true;
        ws2.Range("A3:C3").Style.Fill.BackgroundColor = XLColor.FromArgb(79, 70, 229);
        ws2.Range("A3:C3").Style.Font.FontColor = XLColor.White;

        row = 4;
        foreach (var p in topProducts)
        {
            ws2.Cell(row, 1).Value = p.ProductName;
            ws2.Cell(row, 2).Value = p.TotalQuantity;
            ws2.Cell(row, 3).Value = p.TotalRevenue;
            ws2.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        ws2.Columns().AdjustToContents();

        wb.SaveAs(filePath);
        Logger.Information("Excel report exported: {Path}", filePath);
        return filePath;
    }
}
