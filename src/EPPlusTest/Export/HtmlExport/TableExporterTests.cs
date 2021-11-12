﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using OfficeOpenXml.Export.HtmlExport;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using OfficeOpenXml.Style;
namespace EPPlusTest.Export.HtmlExport
{
    [TestClass]
    public class TableExporterTests : TestBase
    {
#if !NET35 && !NET40
        [TestMethod]
        public void ShouldExportHeadersAsync()
        {
            using(var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Test");
                sheet.Cells["A1"].Value = "Name";
                sheet.Cells["B1"].Value = "Age";
                sheet.Cells["A2"].Value = "John Doe";
                sheet.Cells["B2"].Value = "23";
                var table = sheet.Tables.Add(sheet.Cells["A1:B2"], "myTable");
                table.TableStyle = TableStyles.Dark1;
                table.ShowHeader = true;
                using(var ms = new MemoryStream())
                {
                    table.HtmlExporter.RenderHtmlAsync(ms).Wait();
                    var sr = new StreamReader(ms);
                    ms.Position = 0;
                    var result = sr.ReadToEnd();
                }
            }
        }
#endif

        [TestMethod]
        public void ShouldExportHeaders()
        {
            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Test");
                sheet.Cells["A1"].Value = "Name";
                sheet.Cells["B1"].Value = "Age";
                sheet.Cells["A2"].Value = "John Doe";
                sheet.Cells["B2"].Value = 23;
                var table = sheet.Tables.Add(sheet.Cells["A1:B2"], "myTable");
                table.TableStyle = TableStyles.Dark1;
                table.ShowHeader = true;
                var options = HtmlTableExportOptions.Default;
                options.TableId = "myTable";
                var html = table.HtmlExporter.GetHtmlString(options);
                using (var ms = new MemoryStream())
                {
                    table.HtmlExporter.RenderHtml(ms);
                    var sr = new StreamReader(ms);
                    ms.Position = 0;
                    var result = sr.ReadToEnd();
                }
            }
        }

        [TestMethod]
        public void ExportAllTableStyles()
        {
            string path = _worksheetPath + "TableStyles";
            CreatePathIfNotExists(path);
            using (var p=OpenPackage("TableStylesToHtml.xlsx", true))
            {
                foreach(TableStyles e in Enum.GetValues(typeof(TableStyles)))
                {
                    if (!(e == TableStyles.Custom || e == TableStyles.None))
                    {
                        var ws = p.Workbook.Worksheets.Add(e.ToString());
                        LoadTestdata(ws);
                        var tbl = ws.Tables.Add(ws.Cells["A1:D101"], $"tbl{e}");
                        tbl.TableStyle = e;

                        var html = tbl.HtmlExporter.GetSinglePage();

                        File.WriteAllText($"{path}\\table-{tbl.StyleName}.html", html);
                    }
                }
                SaveAndCleanup(p);
            }
        }

        [TestMethod]
        public void ExportAllFirstLastTableStyles()
        {
            string path = _worksheetPath + "TableStylesFirstLast";
            CreatePathIfNotExists(path);
            using (var p = OpenPackage("TableStylesToHtmlFirstLastCol.xlsx", true))
            {
                foreach (TableStyles e in Enum.GetValues(typeof(TableStyles)))
                {
                    if (!(e == TableStyles.Custom || e == TableStyles.None))
                    {
                        var ws = p.Workbook.Worksheets.Add(e.ToString());
                        LoadTestdata(ws);
                        var tbl = ws.Tables.Add(ws.Cells["A1:D101"], $"tbl{e}");
                        tbl.ShowFirstColumn = true;
                        tbl.ShowLastColumn = true;
                        tbl.TableStyle = e;

                        var html = tbl.HtmlExporter.GetSinglePage();

                        File.WriteAllText($"{path}\\table-{tbl.StyleName}.html", html);
                    }
                }
                SaveAndCleanup(p);
            }
        }
        [TestMethod]
        public void ExportAllCustomTableStyles()
        {
            string path = _worksheetPath + "TableStylesCustomFills";
            CreatePathIfNotExists(path);
            using (var p = OpenPackage("TableStylesToHtmlPatternFill.xlsx", true))
            {
                foreach (ExcelFillStyle fs in Enum.GetValues(typeof(ExcelFillStyle)))
                {
                    var ws = p.Workbook.Worksheets.Add($"PatterFill-{fs}");
                    LoadTestdata(ws);
                    var ts = p.Workbook.Styles.CreateTableStyle($"CustomPattern-{fs}", TableStyles.Medium9);
                    ts.FirstRowStripe.Style.Fill.Style = eDxfFillStyle.PatternFill;
                    ts.FirstRowStripe.Style.Fill.PatternType = fs;
                    ts.FirstRowStripe.Style.Fill.PatternColor.Tint=0.10;
                    var tbl = ws.Tables.Add(ws.Cells["A1:D101"], $"tbl{fs}");
                    tbl.StyleName = ts.Name;

                    var html = tbl.HtmlExporter.GetSinglePage();
                    File.WriteAllText($"{path}\\table-{tbl.StyleName}.html", html);
                }
                SaveAndCleanup(p);
            }
        }

    }
}