﻿/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  01/27/2020         EPPlus Software AB       Initial release EPPlus 5
 *************************************************************************************************/
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Metadata;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup.LookupUtils;
using OfficeOpenXml.FormulaParsing.FormulaExpressions;
using OfficeOpenXml.FormulaParsing.Ranges;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup
{
    [FunctionMetadata(
        Category = ExcelFunctionCategory.LookupAndReference,
        EPPlusVersion = "7",
        Description = "Returns a list of unique values in a list or range",
        SupportsArrays = true)]
    internal class Unique : ExcelFunction
    {
        private readonly LookupComparer _comparer = new LookupComparer(LookupMatchMode.ExactMatch);
        public override CompileResult Execute(IEnumerable<FunctionArgument> arguments, ParsingContext context)
        {
            ValidateArguments(arguments, 1);
            var arg1 = arguments.ElementAt(0);
            if (!arg1.IsExcelRange) return CreateResult(eErrorType.Value);
            var range = arg1.ValueAsRangeInfo;

            var byCol = false;
            if(arguments.Count() > 1)
            {
                byCol = ArgToBool(arguments, 1);
            }
            var exactlyOnce = false;
            if(arguments.Count() > 2)
            {
                exactlyOnce= ArgToBool(arguments, 2);
            }
            var resultRange = byCol ? GetByCols(range, exactlyOnce) : GetByRows(range, exactlyOnce);
            return CreateResult(resultRange, DataType.ExcelRange);
        }

        private bool ListContainsArray(List<object[]> items, object[] candidate, out int collisionIx)
        {
            collisionIx = -1;
            for(var ix = 0; ix < items.Count; ix++)
            {
                var item = items[ix];
                var isEqual = true;
                for(var subIx = 0; subIx < candidate.Length; subIx++)
                {
                    var a = item[subIx];
                    var b = candidate[subIx];
                    if(_comparer.Compare(a, b) != 0)
                    {
                        isEqual = false;
                        break;
                    }
                }
                if (isEqual)
                {
                    collisionIx = ix;
                    return true;
                }
            }
            return false;
        }

        private InMemoryRange GetByRows(IRangeInfo sourceRange, bool exactlyOnce)
        {
            List<object[]> rows = new List<object[]>();
            for(var row = 0;row < sourceRange.Size.NumberOfRows;row++)
            {
                var rowArr = new object[sourceRange.Size.NumberOfCols];
                for (var col = 0; col < sourceRange.Size.NumberOfCols; col++)
                {
                    rowArr[col] = sourceRange.GetOffset(row, col);
                }
                var containsRow = ListContainsArray(rows, rowArr, out int collisionIx);
                if (!containsRow)
                {
                    rows.Add(rowArr);
                }
                else if(exactlyOnce)
                {
                    rows.RemoveAt(collisionIx);
                }
            }
            var result = new InMemoryRange(new RangeDefinition(rows.Count, sourceRange.Size.NumberOfCols));

            for(var row = 0; row < result.Size.NumberOfRows; row++)
            {
                for(var col = 0; col < result.Size.NumberOfCols; col++)
                {
                    result.SetValue(row, col, rows[row][col]);
                }
            }
            return result;
        }

        private InMemoryRange GetByCols(IRangeInfo sourceRange, bool exactlyOnce)
        {
            List<object[]> cols = new List<object[]>();
            for (var col = 0; col < sourceRange.Size.NumberOfCols; col++)
            {
                var colArr = new object[sourceRange.Size.NumberOfRows];
                for (var row = 0; row < sourceRange.Size.NumberOfRows; row++)
                {
                    colArr[row] = sourceRange.GetOffset(row, col);
                }
                var containsCol = ListContainsArray(cols, colArr, out int collisionIx);
                if (!containsCol)
                {
                    cols.Add(colArr);
                }
                else if(exactlyOnce)
                {
                    cols.RemoveAt(collisionIx);
                }
            }
            var result = new InMemoryRange(new RangeDefinition(sourceRange.Size.NumberOfRows, Convert.ToInt16(cols.Count)));

            for (var row = 0; row < result.Size.NumberOfRows; row++)
            {
                for (var col = 0; col < result.Size.NumberOfCols; col++)
                {
                    result.SetValue(row, col, cols[col][row]);
                }
            }
            return result;
        }
    }
}
