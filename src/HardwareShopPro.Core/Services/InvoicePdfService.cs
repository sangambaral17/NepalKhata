using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using HardwareShopPro.Core.Models;
using Serilog;

namespace HardwareShopPro.Core.Services;

/// <summary>
/// Generates professional PDF invoices using PdfSharpCore.
/// Branded for ShopProNepal.
/// </summary>
public static class InvoicePdfService
{
    private static readonly ILogger Logger = Log.ForContext(typeof(InvoicePdfService));

    // ─── Brand Colors ─────────────────────────────────────────────────
    private static readonly XColor Primary = XColor.FromArgb(37, 99, 235);   // Blue-600
    private static readonly XColor PrimaryDark = XColor.FromArgb(29, 78, 216);
    private static readonly XColor HeaderBg = XColor.FromArgb(241, 245, 249); // Slate-100
    private static readonly XColor RowAltBg = XColor.FromArgb(248, 250, 252); // Slate-50
    private static readonly XColor TextDark = XColor.FromArgb(15, 23, 42);    // Slate-900
    private static readonly XColor TextMuted = XColor.FromArgb(100, 116, 139);// Slate-500
    private static readonly XColor BorderColor = XColor.FromArgb(226, 232, 240);
    private static readonly XColor SuccessGreen = XColor.FromArgb(22, 163, 74);
    private static readonly XColor DangerRed = XColor.FromArgb(220, 38, 38);

    private const string SOFTWARE_NAME = "ShopProNepal";
    private const string SOFTWARE_COPYRIGHT = "© 2026 ShopProNepal — Retail Management Software";

    /// <summary>
    /// Generate a professional PDF invoice and save to file.
    /// </summary>
    public static string GeneratePdf(Invoice invoice, BusinessProfile business, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var fileName = $"Invoice_{invoice.InvoiceNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(outputDirectory, fileName);

        var doc = new PdfDocument();
        doc.Info.Title = $"Invoice {invoice.InvoiceNumber}";
        doc.Info.Author = business.Name ?? SOFTWARE_NAME;
        doc.Info.Creator = SOFTWARE_NAME;

        var page = doc.AddPage();
        page.Width = XUnit.FromMillimeter(210);  // A4
        page.Height = XUnit.FromMillimeter(297);

        var gfx = XGraphics.FromPdfPage(page);
        double pageW = page.Width.Point;
        double pageH = page.Height.Point;
        double left = 45;
        double right = pageW - 45;
        double width = right - left;
        double y = 0;

        // ─── Fonts ────────────────────────────────────────────────────
        var fontBrand     = new XFont("Arial", 20, XFontStyle.Bold);
        var fontTitle     = new XFont("Arial", 13, XFontStyle.Bold);
        var fontSubtitle  = new XFont("Arial", 10);
        var fontLabel     = new XFont("Arial", 8, XFontStyle.Bold);
        var fontNormal    = new XFont("Arial", 9);
        var fontSmall     = new XFont("Arial", 7);
        var fontBold      = new XFont("Arial", 9, XFontStyle.Bold);
        var fontAmount    = new XFont("Arial", 12, XFontStyle.Bold);
        var fontFooter    = new XFont("Arial", 7, XFontStyle.Italic);

        var brushDark  = new XSolidBrush(TextDark);
        var brushMuted = new XSolidBrush(TextMuted);
        var brushPrimary = new XSolidBrush(Primary);

        // ─── TOP BAR (colored banner) ─────────────────────────────────
        gfx.DrawRectangle(new XSolidBrush(Primary), 0, 0, pageW, 6);
        y = 30;

        // ─── HEADER: Business Info (left) + Invoice Info (right) ──────
        // Business name
        var bizName = !string.IsNullOrEmpty(business.Name) ? business.Name : SOFTWARE_NAME;
        gfx.DrawString(bizName, fontBrand, brushPrimary, left, y);
        y += 24;

        // Business address
        if (!string.IsNullOrEmpty(business.Address))
        {
            gfx.DrawString(business.Address, fontNormal, brushMuted, left, y);
            y += 14;
        }

        // Business contact
        var contacts = new List<string>();
        if (!string.IsNullOrEmpty(business.Phone)) contacts.Add($"Tel: {business.Phone}");
        if (!string.IsNullOrEmpty(business.Email)) contacts.Add(business.Email);
        if (contacts.Count > 0)
        {
            gfx.DrawString(string.Join("  •  ", contacts), fontSmall, brushMuted, left, y);
            y += 12;
        }

        // PAN/GST
        if (!string.IsNullOrEmpty(business.GSTIN))
        {
            gfx.DrawString($"PAN/VAT: {business.GSTIN}", fontLabel, brushDark, left, y);
            y += 12;
        }

        // ─── Right side: INVOICE label + details ──────────────────────
        double rightCol = right - 180;

        // Invoice title badge
        gfx.DrawRectangle(new XSolidBrush(Primary), rightCol, 24, 180, 28);
        gfx.DrawString("INVOICE", new XFont("Arial", 14, XFontStyle.Bold),
            XBrushes.White, rightCol + 90, 44, XStringFormats.Center);

        // Invoice details below badge
        double detY = 62;
        DrawLabelValue(gfx, fontLabel, fontBold, brushMuted, brushDark, rightCol, right, ref detY, "Invoice #", invoice.InvoiceNumber);
        DrawLabelValue(gfx, fontLabel, fontBold, brushMuted, brushDark, rightCol, right, ref detY, "Date", invoice.Date.ToString("dd MMM yyyy"));
        DrawLabelValue(gfx, fontLabel, fontBold, brushMuted, brushDark, rightCol, right, ref detY, "Status", invoice.PaymentStatus.ToString());
        DrawLabelValue(gfx, fontLabel, fontBold, brushMuted, brushDark, rightCol, right, ref detY, "Payment", invoice.PaymentMode.ToString());

        // Ensure y is below both columns
        y = Math.Max(y, detY) + 12;

        // ─── Separator ────────────────────────────────────────────────
        gfx.DrawLine(new XPen(BorderColor, 1), left, y, right, y);
        y += 16;

        // ─── BILL TO Section ──────────────────────────────────────────
        gfx.DrawRectangle(new XSolidBrush(HeaderBg), left, y, width, 20);
        gfx.DrawString("BILL TO", fontLabel, brushPrimary, left + 10, y + 13);
        y += 28;

        var custName = invoice.CustomerName ?? "Walk-in Customer";
        gfx.DrawString(custName, fontBold, brushDark, left + 10, y);
        y += 20;

        // ─── ITEMS TABLE ──────────────────────────────────────────────
        // Column layout: #(30) | Product(flex) | Qty(50) | Rate(85) | Disc(70) | Total(90)
        double colNo = 30;
        double colQty = 50;
        double colRate = 85;
        double colDisc = 70;
        double colTotal = 90;
        double colProduct = width - colNo - colQty - colRate - colDisc - colTotal;

        double[] cols = { colNo, colProduct, colQty, colRate, colDisc, colTotal };
        string[] headers = { "#", "Product / Service", "Qty", "Rate", "Discount", "Amount" };

        // Table header row
        gfx.DrawRectangle(new XSolidBrush(PrimaryDark), left, y, width, 26);
        double hx = left;
        for (int i = 0; i < headers.Length; i++)
        {
            var fmt = i >= 2 ? XStringFormats.TopRight : XStringFormats.TopLeft;
            double pad = i >= 2 ? cols[i] - 8 : 6;
            gfx.DrawString(headers[i], fontLabel, XBrushes.White, hx + pad, y + 16, fmt);
            hx += cols[i];
        }
        y += 30;

        // Table rows
        int rowCount = 0;
        if (invoice.Items != null)
        {
            foreach (var item in invoice.Items)
            {
                rowCount++;

                // Alternating row background
                if (rowCount % 2 == 0)
                    gfx.DrawRectangle(new XSolidBrush(RowAltBg), left, y - 2, width, 24);

                double rx = left;
                string[] rowData =
                {
                    rowCount.ToString(),
                    item.ProductName ?? $"Item #{item.ProductId}",
                    item.Quantity.ToString(),
                    $"{item.Price:N2}",
                    item.Discount > 0 ? $"{item.Discount:N2}" : "-",
                    $"{item.LineTotal:N2}"
                };

                for (int j = 0; j < rowData.Length; j++)
                {
                    var fmt = j >= 2 ? XStringFormats.TopRight : XStringFormats.TopLeft;
                    double pad = j >= 2 ? cols[j] - 8 : 6;
                    var font = (j == 1 || j == rowData.Length - 1) ? fontBold : fontNormal;
                    gfx.DrawString(rowData[j], font, brushDark, rx + pad, y + 14, fmt);
                    rx += cols[j];
                }
                y += 26;

                // Page break safety
                if (y > pageH - 160)
                {
                    page = doc.AddPage();
                    page.Width = XUnit.FromMillimeter(210);
                    page.Height = XUnit.FromMillimeter(297);
                    gfx = XGraphics.FromPdfPage(page);
                    pageH = page.Height.Point;
                    y = 40;
                }
            }
        }

        // Table bottom border
        y += 4;
        gfx.DrawLine(new XPen(BorderColor, 1), left, y, right, y);
        y += 20;

        // ─── TOTALS SECTION (right-aligned) ───────────────────────────
        double totLabelX = right - 190;
        double totValueX = right;

        // Subtotal
        var subTotal = invoice.TotalAmount - invoice.TaxAmount + invoice.DiscountAmount;
        DrawTotalRow(gfx, fontNormal, fontBold, brushMuted, brushDark, totLabelX, totValueX, ref y,
            "Subtotal", $"NPR {subTotal:N2}");

        // Discount (if any)
        if (invoice.DiscountAmount > 0)
        {
            DrawTotalRow(gfx, fontNormal, fontBold, brushMuted, new XSolidBrush(DangerRed), totLabelX, totValueX, ref y,
                "Discount", $"- NPR {invoice.DiscountAmount:N2}");
        }

        // Tax (show only if > 0)
        if (invoice.TaxAmount > 0)
        {
            DrawTotalRow(gfx, fontNormal, fontBold, brushMuted, brushDark, totLabelX, totValueX, ref y,
                "VAT (13%)", $"NPR {invoice.TaxAmount:N2}");
        }

        y += 4;

        // ─── GRAND TOTAL (highlighted box) ────────────────────────────
        double gtWidth = 200;
        double gtX = right - gtWidth;
        gfx.DrawRectangle(new XSolidBrush(Primary), gtX, y, gtWidth, 32);
        gfx.DrawString("GRAND TOTAL", new XFont("Arial", 10, XFontStyle.Bold),
            XBrushes.White, gtX + 12, y + 20);
        gfx.DrawString($"NPR {invoice.TotalAmount:N2}", fontAmount,
            XBrushes.White, right - 8, y + 22, XStringFormats.TopRight);
        y += 44;

        // ─── AMOUNT IN WORDS ──────────────────────────────────────────
        var words = NumberToWordsConverter.Convert(invoice.TotalAmount);
        gfx.DrawString("Amount in Words:", fontLabel, brushMuted, left, y);
        y += 14;
        gfx.DrawString(words, new XFont("Arial", 9, XFontStyle.Italic), brushDark, left, y);
        y += 30;

        // ─── SIGNATURE LINE ──────────────────────────────────────────
        if (y < pageH - 120)
        {
            double sigY = pageH - 110;
            gfx.DrawLine(new XPen(BorderColor, 0.5), right - 160, sigY, right, sigY);
            gfx.DrawString("Authorized Signature", fontSmall, brushMuted, right - 80, sigY + 10, XStringFormats.TopCenter);
        }

        // ─── FOOTER ──────────────────────────────────────────────────
        double footY = pageH - 50;
        gfx.DrawLine(new XPen(BorderColor, 0.5), left, footY, right, footY);
        footY += 10;
        gfx.DrawString("Thank you for your business!", new XFont("Arial", 9, XFontStyle.Bold),
            brushPrimary, pageW / 2, footY, XStringFormats.TopCenter);
        footY += 14;
        gfx.DrawString("This is a computer-generated invoice and does not require a physical signature.",
            fontFooter, brushMuted, pageW / 2, footY, XStringFormats.TopCenter);
        footY += 12;
        gfx.DrawString(SOFTWARE_COPYRIGHT, fontFooter, brushMuted, pageW / 2, footY, XStringFormats.TopCenter);

        // ─── Bottom color bar ─────────────────────────────────────────
        gfx.DrawRectangle(new XSolidBrush(Primary), 0, pageH - 6, pageW, 6);

        // ─── Save ─────────────────────────────────────────────────────
        doc.Save(filePath);
        Logger.Information("PDF invoice generated: {FilePath}", filePath);
        return filePath;
    }

    // ─── Helper: Draw a label-value pair on the same row ──────────────
    private static void DrawLabelValue(XGraphics gfx, XFont labelFont, XFont valueFont,
        XSolidBrush labelBrush, XSolidBrush valueBrush,
        double labelX, double valueX, ref double y, string label, string value)
    {
        gfx.DrawString($"{label}:", labelFont, labelBrush, labelX, y);
        gfx.DrawString(value, valueFont, valueBrush, valueX, y, XStringFormats.TopRight);
        y += 16;
    }

    // ─── Helper: Draw a totals row ────────────────────────────────────
    private static void DrawTotalRow(XGraphics gfx, XFont labelFont, XFont valueFont,
        XSolidBrush labelBrush, XSolidBrush valueBrush,
        double labelX, double valueX, ref double y, string label, string value)
    {
        gfx.DrawString(label, labelFont, labelBrush, labelX, y);
        gfx.DrawString(value, valueFont, valueBrush, valueX, y, XStringFormats.TopRight);
        y += 20;
    }
}
