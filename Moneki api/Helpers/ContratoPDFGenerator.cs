using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

public static class ContratoPdfGenerator
{
    public static byte[] GenerarContrato(string vendedor, string comprador, string tipoBien, decimal monto)
    {
        using (var document = new PdfDocument())
        {
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            
            // 🔥 USAR FUENTES QUE EXISTEN EN LINUX
            // Opciones: "LiberationSans", "DejaVuSans", "Arial", "Courier New"
            var fontTitulo = new XFont("LiberationSans", 18, XFontStyle.Bold);
            var fontTexto = new XFont("LiberationSans", 12, XFontStyle.Regular);
            var fontNegrita = new XFont("LiberationSans", 12, XFontStyle.Bold);
            

        double y = 40;

        // ===== TITULO =====
        gfx.DrawString(
            "CONTRATO DE COMPRAVENTA",
            tituloFont,
            XBrushes.Black,
            new XRect(0, y, page.Width, 30),
            XStringFormats.TopCenter);

        y += 60;

        // ===== DATOS =====
        gfx.DrawString($"VENDEDOR: {vendedor}", textoFont, XBrushes.Black, 40, y); y += 20;
        gfx.DrawString($"COMPRADOR: {comprador}", textoFont, XBrushes.Black, 40, y); y += 20;
        gfx.DrawString($"BIEN: {tipoBien}", textoFont, XBrushes.Black, 40, y); y += 20;
        gfx.DrawString($"PRECIO: ${monto:N2}", textoFont, XBrushes.Black, 40, y); y += 30;

        // ===== TEXTO DEL CONTRATO =====
        gfx.DrawString(
            "Ambas partes declaran que celebran el presente contrato de compraventa conforme al Código Civil vigente y aceptan los términos aquí establecidos.",
            textoFont,
            XBrushes.Black,
            new XRect(40, y, page.Width - 80, 200),
            XStringFormats.TopLeft);

        // ===== FIRMAS =====
        double firmaY = page.Height - 160;

        // Linea vendedor
        gfx.DrawLine(XPens.Black, 80, firmaY, 240, firmaY);
        gfx.DrawString("Firma del Vendedor", textoFont, XBrushes.Black, 100, firmaY + 20);

        // Linea comprador
        gfx.DrawLine(XPens.Black, page.Width - 240, firmaY, page.Width - 80, firmaY);
        gfx.DrawString("Firma del Comprador", textoFont, XBrushes.Black, page.Width - 220, firmaY + 20);

        // Footer
        gfx.DrawString(
            "Contrato generado por el sistema MONEKI",
            smallFont,
            XBrushes.Black,
            new XRect(0, page.Height - 40, page.Width, 20),
            XStringFormats.Center);

        using var ms = new MemoryStream();
        document.Save(ms);
        return ms.ToArray();
    }
    
}
