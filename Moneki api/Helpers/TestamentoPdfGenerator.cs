using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using System.IO;

namespace Proyecto_servicio.Helpers
{
    public static class TestamentoPdfGenerator
    {
        public static byte[] GenerarTestamento(
            string nombreCompleto,
            string estadoCivil,
            bool tieneHijos,
            int numeroHijos,
            string bienesDeclarados,
            DateTime fecha
        )
        {
            using var document = new PdfDocument();
            var page = document.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            var gfx = XGraphics.FromPdfPage(page);

            var tituloFont = new XFont("Arial", 18, XFontStyle.Bold);
            var textoFont = new XFont("Arial", 12);
            var smallFont = new XFont("Arial", 10, XFontStyle.Italic);

            double y = 40;

            // ===== TÍTULO =====
            gfx.DrawString(
                "TESTAMENTO",
                tituloFont,
                XBrushes.Black,
                new XRect(0, y, page.Width, 30),
                XStringFormats.TopCenter);

            y += 50;

            // ===== DATOS GENERALES =====
            gfx.DrawString($"Nombre completo: {nombreCompleto}", textoFont, XBrushes.Black, 40, y); y += 20;
            gfx.DrawString($"Estado civil: {estadoCivil}", textoFont, XBrushes.Black, 40, y); y += 20;
            gfx.DrawString(
                tieneHijos
                    ? $"Hijos: Sí ({numeroHijos})"
                    : "Hijos: No",
                textoFont,
                XBrushes.Black,
                40,
                y);

            y += 30;

            // ===== BIENES =====
            gfx.DrawString(
                "Bienes declarados:",
                textoFont,
                XBrushes.Black,
                40,
                y);

            y += 20;

            gfx.DrawString(
                bienesDeclarados,
                textoFont,
                XBrushes.Black,
                new XRect(40, y, page.Width - 80, 200),
                XStringFormats.TopLeft);

            // ===== FECHA =====
            gfx.DrawString(
                $"Fecha de elaboración: {fecha:dd/MM/yyyy}",
                smallFont,
                XBrushes.Black,
                40,
                page.Height - 120);

            // ===== FIRMA =====
            gfx.DrawLine(
                XPens.Black,
                80,
                page.Height - 80,
                260,
                page.Height - 80);

            gfx.DrawString(
                "Firma del otorgante",
                textoFont,
                XBrushes.Black,
                100,
                page.Height - 60);

            // ===== FOOTER =====
            gfx.DrawString(
                "Documento generado por el sistema MONEKI",
                smallFont,
                XBrushes.Black,
                new XRect(0, page.Height - 30, page.Width, 20),
                XStringFormats.Center);

            using var ms = new MemoryStream();
            document.Save(ms);
            return ms.ToArray();
        }
    }
}
