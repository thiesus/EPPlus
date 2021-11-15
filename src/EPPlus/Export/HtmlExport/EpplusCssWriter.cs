﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  11/07/2021         EPPlus Software AB       Added Html Export
 *************************************************************************************************/
using OfficeOpenXml.Style;
using OfficeOpenXml.Style.Table;
using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Drawing.Style.Coloring;
using OfficeOpenXml.Table;
using System.Drawing;
using OfficeOpenXml.Drawing.Theme;
using OfficeOpenXml.Style.Dxf;
using static OfficeOpenXml.Export.HtmlExport.ColumnDataTypeManager;
using System.Text;

namespace OfficeOpenXml.Export.HtmlExport
{
    internal class EpplusCssWriter
    {
        readonly Stream _stream;
        readonly StreamWriter _writer;
        readonly Stack<string> _elementStack = new Stack<string>();
        private readonly List<EpplusHtmlAttribute> _attributes = new List<EpplusHtmlAttribute>();

        const string IndentWhiteSpace = "  ";
        private bool _newLine;
        ExcelTable _table;
        ExcelTheme _theme;
        public EpplusCssWriter(Stream stream, ExcelTable table)
        {
            _stream = stream;
            _table = table;
            if(table.WorkSheet.Workbook.ThemeManager.CurrentTheme == null)
            {
                table.WorkSheet.Workbook.ThemeManager.CreateDefaultTheme();
            }
            _theme = table.WorkSheet.Workbook.ThemeManager.CurrentTheme;
            _writer = new StreamWriter(stream);
        }
        internal int Indent { get; set; }
        internal void RenderGenericCss()
        {
            _writer.Write("table.epplus-table{border-spacing:0px;border-collapse:collapse;font-family:calibri;font-size:11pt}");
        }
        internal void RenderCss(List<string> datatypes) 
        {
            ExcelTableNamedStyle tblStyle;
            if (_table.TableStyle == TableStyles.Custom)
            {
                tblStyle = _table.WorkSheet.Workbook.Styles.TableStyles[_table.StyleName].As.TableStyle;
            }
            else
            {
                var tmpNode = _table.WorkSheet.Workbook.StylesXml.CreateElement("c:tableStyle");
                tblStyle = new ExcelTableNamedStyle(_table.WorkSheet.Workbook.Styles.NameSpaceManager, tmpNode, _table.WorkSheet.Workbook.Styles);
                tblStyle.SetFromTemplate(_table.TableStyle);
            }

            var tableClass = $"epplus-tablestyle-{tblStyle.Name.ToLower()}";
            AddAlignmentToCss($"{tableClass}", datatypes);
            AddToCss($"{tableClass}", tblStyle.WholeTable, "");
            AddToCssBorderVH($"{tableClass}", tblStyle.WholeTable, "");

            if (_table.ShowHeader)
            {
                AddToCss($"{tableClass}", tblStyle.HeaderRow, " thead tr th");
                AddToCss($"{tableClass}", tblStyle.FirstHeaderCell, " thead tr th:first-child");
                if (_table.Columns.Count > 1)
                {
                    AddToCss($"{tableClass}", tblStyle.LastTotalCell, $" thead tr th:last-child)");
                }
            }

            if (_table.ShowTotal)
            {
                AddToCss($"{tableClass}", tblStyle.TotalRow, " tfoot tr td");
                AddToCss($"{tableClass}", tblStyle.FirstTotalCell, " tfoot tr td:first-child");
                if (_table.Columns.Count > 1)
                {
                    AddToCss($"{tableClass}", tblStyle.LastTotalCell, $" tfoot tr td:last-child)");
                }
            }

            if(_table.ShowColumnStripes)
            {
                AddToCss($"{tableClass}", tblStyle.FirstColumnStripe, $" tbody tr td:nth-child(odd)");
                if(tblStyle.SecondColumnStripe.Style.HasValue)
                {
                    AddToCss($"{tableClass}",  tblStyle.SecondColumnStripe, $" tbody tr td:nth-child(even)");
                }
                else
                {
                    AddToCss($"{tableClass}", tblStyle.WholeTable, $" tbody tr td:nth-child(even)", true, true, false);
                }
            }

            if (_table.ShowRowStripes)
            {
                AddToCss($"{tableClass}", tblStyle.FirstRowStripe, " tbody tr:nth-child(odd) td");
                if(tblStyle.SecondRowStripe.Style.HasValue)
                {
                    AddToCss($"{tableClass}", tblStyle.SecondRowStripe, " tbody tr:nth-child(even) td");
                }
                else
                {
                    AddToCss($"{tableClass}", tblStyle.WholeTable, " tbody tr:nth-child(even) td", true, true, false);
                }
            }
            else
            {
                AddToCss($"{tableClass}", tblStyle.FirstRowStripe, " thead tr td");
            }

            if (_table.ShowLastColumn && _table.Columns.Count > 1)
            {
                AddToCss($"{tableClass}", tblStyle.LastColumn, $" tbody tr td:last-child");
            }

            if (_table.ShowFirstColumn)
            {
                AddToCss($"{tableClass}", tblStyle.FirstColumn, " tbody tr td:first-child");
            }

            _writer.Flush();
        }

        private void AddAlignmentToCss(string name, List<string> dataTypes)
        {
            var row = _table.ShowHeader ? _table.Address._fromRow + 1 : _table.Address._fromRow;
            for (int c=0;c < _table.Columns.Count;c++)
            {
                var col = _table.Address._fromCol + c;
                var styleId = _table.WorkSheet.GetStyleInner(row, col);
                string hAlign = "";
                string vAlign = "";
                if(styleId>0)
                {
                    var xfs = _table.WorkSheet.Workbook.Styles.CellXfs[styleId];
                    if(xfs.ApplyAlignment)
                    {
                        switch(xfs.HorizontalAlignment)
                        {
                            case ExcelHorizontalAlignment.Right:
                                hAlign = "right";
                                break;
                            case ExcelHorizontalAlignment.Center:
                            case ExcelHorizontalAlignment.CenterContinuous:
                                hAlign = "center";
                                break;
                            case ExcelHorizontalAlignment.Left:
                                hAlign = "left";
                                break;
                        }
                        switch(xfs.VerticalAlignment)
                        {
                            case ExcelVerticalAlignment.Top:
                                vAlign = "top";
                                break;
                            case ExcelVerticalAlignment.Center:
                                vAlign = "middle";
                                break;
                            case ExcelVerticalAlignment.Bottom:
                                vAlign = "bottom";
                                break;
                        }
                    }
                }

                if(string.IsNullOrEmpty(hAlign))
                {
                    if (dataTypes[c] == HtmlDataTypes.Number)
                    {
                        hAlign = "right";
                    }
                }

                if(!(string.IsNullOrEmpty(hAlign) && string.IsNullOrEmpty(vAlign)))
                {                    
                    _writer.Write($"table.{name} td,th:nth-child({col+1})");
                    _writer.Write("{");
                    if (string.IsNullOrEmpty(hAlign)==false)
                    {
                        _writer.Write($"text-align:{hAlign};");
                    }
                    if (string.IsNullOrEmpty(vAlign) == false)
                    {
                        _writer.Write($"vertical-align:{vAlign};");
                    }
                    _writer.Write("}");
                }
            }
        }

        private void AddToCss(string name, ExcelTableStyleElement element, string htmlElement, bool writeFill = true, bool writeFont = true, bool writeBorder=true)
        {
            var s = element.Style;
            if (s.HasValue == false) return; //Dont add empty elements
            _writer.Write($"table.{name}{htmlElement}");
            _writer.Write("{");
            if (writeFill) WriteFillStyles(s.Fill);
            if (writeFont) WriteFontStyles(s.Font);
            if (writeBorder) WriteBorderStyles(s.Border);
            _writer.Write("}");
        }
        private void AddToCssBorderVH(string name, ExcelTableStyleElement element, string htmlElement)
        {
            var s = element.Style;
            if (s.Border.Vertical.HasValue == false && s.Border.Horizontal.HasValue==false) return; //Dont add empty elements
            _writer.Write($"table.{name}{htmlElement} td,tr");
            _writer.Write("{");
            WriteBorderStylesVerticalHorizontal(s.Border);
            _writer.Write("}");
        }
        private void WriteFillStyles(ExcelDxfFill f)
        {
            if (f.HasValue)
            {
                if (f.Style == eDxfFillStyle.PatternFill)
                {
                    if (f.PatternType.Value==ExcelFillStyle.Solid)
                    {
                        _writer.Write($"background-color:{GetDxfColor(f.BackgroundColor)};");
                    }
                    else
                    {
                        _writer.Write($"{GetPatternSvg(f)};");
                    }
                }
            }
        }

        private object GetPatternSvg(ExcelDxfFill f)
        {
            string svg;
            switch(f.PatternType)
            {
                case ExcelFillStyle.DarkGray:
                    svg = string.Format(PatternFills.Dott75, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.MediumGray:
                    svg = string.Format(PatternFills.Dott50, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightGray:
                    svg = string.Format(PatternFills.Dott25, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.Gray125:
                    svg=string.Format(PatternFills.Dott12_5, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.Gray0625:
                    svg = string.Format(PatternFills.Dott6_25, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.DarkHorizontal:
                    svg = string.Format(PatternFills.HorizontalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.DarkVertical:
                    svg = string.Format(PatternFills.VerticalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightHorizontal:
                    svg = string.Format(PatternFills.ThinHorizontalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightVertical:
                    svg = string.Format(PatternFills.ThinVerticalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.DarkDown:
                    svg = string.Format(PatternFills.ReverseDiagonalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.DarkUp:
                    svg = string.Format(PatternFills.DiagonalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightDown:
                    svg = string.Format(PatternFills.ThinReverseDiagonalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightUp:
                    svg = string.Format(PatternFills.ThinDiagonalStripe, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.DarkGrid:
                    svg = string.Format(PatternFills.DiagonalCrosshatch, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.DarkTrellis:
                    svg = string.Format(PatternFills.ThickDiagonalCrosshatch, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightGrid:
                    svg = string.Format(PatternFills.ThinHorizontalCrosshatch, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                case ExcelFillStyle.LightTrellis:
                    svg = string.Format(PatternFills.ThinDiagonalCrosshatch, GetDxfColor(f.BackgroundColor), GetDxfColor(f.PatternColor));
                    break;
                default:
                    return "";
            }
            
            return $"background-repeat:repeat;background:url(data:image/svg+xml;base64,{Convert.ToBase64String(Encoding.ASCII.GetBytes(svg))});";
        }

        private void WriteFontStyles(ExcelDxfFontBase f)
        {
            if (f.Color.HasValue)
            {
                _writer.Write($"color:{GetDxfColor(f.Color)};");
                //color: #007731;
            }
            if (f.Bold.HasValue && f.Bold.Value)
            {
                _writer.Write("font-weight:bolder;");
            }
            if (f.Italic.HasValue && f.Italic.Value)
            {
                _writer.Write("font-style:italic;");
            }
            if (f.Strike.HasValue && f.Strike.Value)
            {
                _writer.Write("text-decoration:line-through solid;");
            }
            if (f.Underline.HasValue && f.Underline != ExcelUnderLineType.None)
            {
                _writer.Write("text-decoration:underline ");
                switch (f.Underline.Value)
                {
                    case ExcelUnderLineType.Double:
                    case ExcelUnderLineType.DoubleAccounting:
                        _writer.Write("double;");
                        break;
                    default:
                        _writer.Write("solid;");
                        break;
                }
            }
            if (f.Underline.HasValue && f.Underline != ExcelUnderLineType.None)
            {
                _writer.Write("text-decoration:underline ");
                switch (f.Underline.Value)
                {
                    case ExcelUnderLineType.Double:
                    case ExcelUnderLineType.DoubleAccounting:
                        _writer.Write("double;");
                        break;
                    default:
                        _writer.Write("solid;");
                        break;
                }
            }
        }
        private void WriteBorderStyles(ExcelDxfBorderBase b)
        {
            if (b.HasValue)
            {
                WriteBorderItem(b.Top, "top");
                WriteBorderItem(b.Bottom, "bottom");
                WriteBorderItem(b.Left, "left");
                WriteBorderItem(b.Right, "right");
            }
        }
        private void WriteBorderStylesVerticalHorizontal(ExcelDxfBorderBase b)
        {
            if (b.HasValue)
            {
                WriteBorderItem(b.Vertical, "top");
                WriteBorderItem(b.Vertical, "bottom");
                WriteBorderItem(b.Horizontal, "left");
                WriteBorderItem(b.Horizontal, "right");
            }
        }

        private void WriteBorderItem(ExcelDxfBorderItem bi, string suffix)
        {
            if (bi.HasValue && bi.Style != ExcelBorderStyle.None)
            {
                _writer.Write($"border-{suffix}:");
                switch (bi.Style)
                {
                    case ExcelBorderStyle.Hair:
                        _writer.Write($"1px solid");
                        break;
                    case ExcelBorderStyle.Thin:
                        _writer.Write($"thin solid");
                        break;
                    case ExcelBorderStyle.Medium:
                        _writer.Write($"medium solid");
                        break;
                    case ExcelBorderStyle.Thick:
                        _writer.Write($"thick solid");
                        break;
                    case ExcelBorderStyle.Double:
                        _writer.Write($"double");
                        break;
                    case ExcelBorderStyle.Dotted:
                        _writer.Write($"dotted");
                        break;
                    case ExcelBorderStyle.Dashed:
                    case ExcelBorderStyle.DashDot:
                    case ExcelBorderStyle.DashDotDot:
                        _writer.Write($"dashed");
                        break;
                    case ExcelBorderStyle.MediumDashed:
                    case ExcelBorderStyle.MediumDashDot:
                    case ExcelBorderStyle.MediumDashDotDot:
                        _writer.Write($"medium dashed");
                        break;
                }
                if (bi.Color.HasValue)
                {
                    _writer.Write($" {GetDxfColor(bi.Color)}");
                }
                _writer.Write(";");
            }
        }
        private string GetDxfColor(Style.Dxf.ExcelDxfColor c)
        {
            Color ret;
            if (c.Color.HasValue)
            {
                ret = c.Color.Value;
            }
            else if (c.Theme.HasValue)
            {
                ret = GetThemeColor(c.Theme.Value);
            }
            else if (c.Index.HasValue)
            {
                ret = ExcelColor.GetIndexedColor(c.Index.Value);
            }
            else
            {
                //Automatic, set to black.
                ret = Color.Black;
            }
            if (c.Tint.HasValue)
            {
                ret = ApplyTint(ret, c.Tint.Value);
            }
            return "#" + ret.ToArgb().ToString("x8").Substring(2);
        }

        internal Color ApplyTint(Color ret, double tint)
        {
            if (tint == 0)
            {
                return ret;
            }
            else
            {
                ExcelDrawingRgbColor.GetHslColor(ret, out double h, out double s, out double l);
                if (tint < 0)
                {
                    l = l*(1.0 + tint);
                }
                else if (tint > 0)
                {
                    //l = (1-l)*tint;
                    //l = 1 - l * (1 - tint);
                    l += (1-l) * tint;
                }
                return ExcelDrawingHslColor.GetRgb(h, s, l);
            }
        }

        private Color GetThemeColor(eThemeSchemeColor tc)
        {
            var cm = _theme.ColorScheme.GetColorByEnum(tc);
            return GetThemeColor(cm);
        }

        private Color GetThemeColor(ExcelDrawingThemeColorManager cm)
        {
            Color color;
            switch (cm.ColorType)
            {
                case eDrawingColorType.Rgb:
                    color = cm.RgbColor.Color;
                    break;
                case eDrawingColorType.Preset:
                    color = Color.FromName(cm.PresetColor.Color.ToString());
                    break;
                case eDrawingColorType.System:
                    color = cm.SystemColor.GetColor();
                    break;
                case eDrawingColorType.RgbPercentage:
                    var rp = cm.RgbPercentageColor;
                    color = Color.FromArgb(GetRgpPercentToRgb(rp.RedPercentage),
                                   GetRgpPercentToRgb(rp.GreenPercentage),
                                   GetRgpPercentToRgb(rp.BluePercentage));
                    break;
                case eDrawingColorType.Hsl:
                    color = cm.HslColor.GetRgbColor();
                    break;
                default:
                    color = Color.Empty;
                    break;
            }

            //TODO:Apply Transforms

            return color;
        }

        private int GetRgpPercentToRgb(double percentage)
        {
            if (percentage < 0) return 0;
            if (percentage > 255) return 255;
            return (int)(percentage * 255 / 100);
        }

    }
}
