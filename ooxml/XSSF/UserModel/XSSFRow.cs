/* ====================================================================
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for Additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
==================================================================== */

using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using NPOI.XSSF.Model;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using NPOI.SS.UserModel;
using System.Collections.Generic;
using NPOI.Util;
using NPOI.SS;
using System.Collections;
using NPOI.XSSF.UserModel.Helpers;
using NPOI.SS.Formula;
using System.Linq;

namespace NPOI.XSSF.UserModel
{

    /**
     * High level representation of a row of a spreadsheet.
     */
    public class XSSFRow : IRow, IComparable<XSSFRow>
    {
        private static POILogger _logger = POILogFactory.GetLogger(typeof(XSSFRow));

        /**
         * the xml bean Containing all cell defInitions for this row
         */
        private CT_Row _row;

        /**
         * Cells of this row keyed by their column indexes.
         * The TreeMap ensures that the cells are ordered by columnIndex in the ascending order.
         */
        private SortedDictionary<int, ICell> _cells;
        /**
         * the parent sheet
         */
        private XSSFSheet _sheet;

        /**
         * Construct a XSSFRow.
         *
         * @param row the xml bean Containing all cell defInitions for this row.
         * @param sheet the parent sheet.
         */
        public XSSFRow(CT_Row row, XSSFSheet sheet)
        {
            this._row = row;
            this._sheet = sheet;
            this._cells = new SortedDictionary<int, ICell>();
            if (0 < row.SizeOfCArray())
            {
                foreach (CT_Cell c in row.c)
                {
                    XSSFCell cell = new XSSFCell(this, c);
                    _cells.Add(cell.ColumnIndex,cell);
                    sheet.OnReadCell(cell);
                }
            }

            if (!row.IsSetR())
            {
                // Certain file format writers skip the row number
                // Assume no gaps, and give this the next row number
                int nextRowNum = sheet.LastRowNum + 2;
                if (nextRowNum == 2 && sheet.PhysicalNumberOfRows == 0)
                {
                    nextRowNum = 1;
                }
                row.r = (uint)nextRowNum;
            }
        }

        /**
         * Returns the XSSFSheet this row belongs to
         *
         * @return the XSSFSheet that owns this row
         */
        public ISheet Sheet
        {
            get
            {
                return this._sheet;
            }
        }

        /**
         * Cell iterator over the physically defined cells:
         * <blockquote><pre>
         * for (Iterator<Cell> it = row.cellIterator(); it.HasNext(); ) {
         *     Cell cell = it.next();
         *     ...
         * }
         * </pre></blockquote>
         *
         * @return an iterator over cells in this row.
         */
        public SortedDictionary<int, ICell>.ValueCollection.Enumerator CellIterator()
        {
            return _cells.Values.GetEnumerator();
        }

        /**
         * Alias for {@link #cellIterator()} to allow  foreach loops:
         * <blockquote><pre>
         * for(Cell cell : row){
         *     ...
         * }
         * </pre></blockquote>
         *
         * @return an iterator over cells in this row.
         */
        public IEnumerator<ICell> GetEnumerator()
        {
            return CellIterator();
        }

        /**
         * Compares two <code>XSSFRow</code> objects.  Two rows are equal if they belong to the same worksheet and
         * their row indexes are equal.
         *
         * @param   row   the <code>XSSFRow</code> to be compared.
         * @return  <ul>
         *      <li>
         *      the value <code>0</code> if the row number of this <code>XSSFRow</code> is
         *      equal to the row number of the argument <code>XSSFRow</code>
         *      </li>
         *      <li>
         *      a value less than <code>0</code> if the row number of this this <code>XSSFRow</code> is
         *      numerically less than the row number of the argument <code>XSSFRow</code>
         *      </li>
         *      <li>
         *      a value greater than <code>0</code> if the row number of this this <code>XSSFRow</code> is
         *      numerically greater than the row number of the argument <code>XSSFRow</code>
         *      </li>
         *      </ul>
         * @throws IllegalArgumentException if the argument row belongs to a different worksheet
         */
        public int CompareTo(XSSFRow other)
        {
            if (this.Sheet != other.Sheet)
            {
                throw new ArgumentException("The compared rows must belong to the same sheet");
            }

            return RowNum.CompareTo(other.RowNum);
        }

        public override bool Equals(Object obj)
        {
            if (!(obj is XSSFRow))
            {
                return false;
            }
            XSSFRow other = (XSSFRow)obj;

            return (this.RowNum == other.RowNum) &&
                   (this.Sheet == other.Sheet);
        }

        public override int GetHashCode()
        {
            return _row.GetHashCode();
        }

        /**
         * Use this to create new cells within the row and return it.
         * <p>
         * The cell that is returned is a {@link Cell#CELL_TYPE_BLANK}. The type can be Changed
         * either through calling <code>SetCellValue</code> or <code>SetCellType</code>.
         * </p>
         * @param columnIndex - the column number this cell represents
         * @return Cell a high level representation of the Created cell.
         * @throws ArgumentException if columnIndex < 0 or greater than 16384,
         *   the maximum number of columns supported by the SpreadsheetML format (.xlsx)
         */
        public ICell CreateCell(int columnIndex)
        {
            return CreateCell(columnIndex, CellType.Blank);
        }

        /**
         * Use this to create new cells within the row and return it.
         *
         * @param columnIndex - the column number this cell represents
         * @param type - the cell's data type
         * @return XSSFCell a high level representation of the Created cell.
         * @throws ArgumentException if the specified cell type is invalid, columnIndex < 0
         *   or greater than 16384, the maximum number of columns supported by the SpreadsheetML format (.xlsx)
         * @see Cell#CELL_TYPE_BLANK
         * @see Cell#CELL_TYPE_BOOLEAN
         * @see Cell#CELL_TYPE_ERROR
         * @see Cell#CELL_TYPE_FORMULA
         * @see Cell#CELL_TYPE_NUMERIC
         * @see Cell#CELL_TYPE_STRING
         */
        public ICell CreateCell(int columnIndex, CellType type)
        {
            CT_Cell ctCell;
            XSSFCell prev = _cells.ContainsKey(columnIndex) ? (XSSFCell)_cells[columnIndex] : null;
            if (prev != null)
            {
                ctCell = prev.GetCTCell();
                ctCell.Set(new CT_Cell());
            }
            else
            {
                ctCell = _row.AddNewC();
            }
            XSSFCell xcell = new XSSFCell(this, ctCell);
            xcell.SetCellNum(columnIndex);
            if (type != CellType.Blank)
            {
                xcell.SetCellType(type);
            }
            _cells[columnIndex] = xcell;
            return xcell;
        }

        /**
         * Returns the cell at the given (0 based) index,
         *  with the {@link NPOI.SS.usermodel.Row.MissingCellPolicy} from the parent Workbook.
         *
         * @return the cell at the given (0 based) index
         */
        public ICell GetCell(int cellnum)
        {
            return GetCell(cellnum, _sheet.Workbook.MissingCellPolicy);
        }
        /// <summary>
        /// Get the hssfcell representing a given column (logical cell)
        /// 0-based. If you ask for a cell that is not defined, then
        /// you Get a null.
        /// This is the basic call, with no policies applied
        /// </summary>
        /// <param name="cellnum">0 based column number</param>
        /// <returns>Cell representing that column or null if Undefined.</returns>
        private ICell RetrieveCell(int cellnum)
        {
            if (!_cells.ContainsKey(cellnum))
                return null;
            //if (cellnum < 0 || cellnum >= cells.Count) return null;
            return _cells[cellnum];
        }
        /**
         * Returns the cell at the given (0 based) index, with the specified {@link NPOI.SS.usermodel.Row.MissingCellPolicy}
         *
         * @return the cell at the given (0 based) index
         * @throws ArgumentException if cellnum < 0 or the specified MissingCellPolicy is invalid
         * @see Row#RETURN_NULL_AND_BLANK
         * @see Row#RETURN_BLANK_AS_NULL
         * @see Row#CREATE_NULL_AS_BLANK
         */
        public ICell GetCell(int cellnum, MissingCellPolicy policy)
        {
            if (cellnum < 0) throw new ArgumentException("Cell index must be >= 0");

            XSSFCell cell = (XSSFCell)RetrieveCell(cellnum);
            switch (policy)
            {
                case MissingCellPolicy.RETURN_NULL_AND_BLANK:
                    return cell;
                case MissingCellPolicy.RETURN_BLANK_AS_NULL:
                    bool isBlank = (cell != null && cell.CellType == CellType.Blank);
                    return (isBlank) ? null : cell;
                case MissingCellPolicy.CREATE_NULL_AS_BLANK:
                    return (cell == null) ? CreateCell(cellnum, CellType.Blank) : cell;
                default:
                    throw new ArgumentException("Illegal policy " + policy + " (" + policy + ")");
            }
        }
        int GetFirstKey()
        {
            return _cells.Keys.Min();
        }
        int GetLastKey()
        {
            return _cells.Keys.Max();
        }
        /**
         * Get the number of the first cell Contained in this row.
         *
         * @return short representing the first logical cell in the row,
         *  or -1 if the row does not contain any cells.
         */
        public short FirstCellNum
        {
            get
            {
                return (short)(_cells.Count == 0 ? -1 : GetFirstKey());
            }
        }

        /**
         * Gets the index of the last cell Contained in this row <b>PLUS ONE</b>. The result also
         * happens to be the 1-based column number of the last cell.  This value can be used as a
         * standard upper bound when iterating over cells:
         * <pre>
         * short minColIx = row.GetFirstCellNum();
         * short maxColIx = row.GetLastCellNum();
         * for(short colIx=minColIx; colIx&lt;maxColIx; colIx++) {
         *   XSSFCell cell = row.GetCell(colIx);
         *   if(cell == null) {
         *     continue;
         *   }
         *   //... do something with cell
         * }
         * </pre>
         *
         * @return short representing the last logical cell in the row <b>PLUS ONE</b>,
         *   or -1 if the row does not contain any cells.
         */
        public short LastCellNum
        {
            get
            {
                return (short)(_cells.Count == 0 ? -1 : (GetLastKey() + 1));
            }
        }

        /**
         * Get the row's height measured in twips (1/20th of a point). If the height is not Set, the default worksheet value is returned,
         * See {@link NPOI.XSSF.usermodel.XSSFSheet#GetDefaultRowHeightInPoints()}
         *
         * @return row height measured in twips (1/20th of a point)
         */
        public short Height
        {
            get
            {
                return (short)(HeightInPoints * 20);
            }
            set 
            {
                if (value < 0)
                {
                    if (_row.IsSetHt()) _row.unSetHt();
                    if (_row.IsSetCustomHeight()) _row.unSetCustomHeight();
                }
                else
                {
                    _row.ht = ((double)value / 20);
                    _row.customHeight = (true);

                }
            }
        }

        /**
         * Returns row height measured in point size. If the height is not Set, the default worksheet value is returned,
         * See {@link NPOI.XSSF.usermodel.XSSFSheet#GetDefaultRowHeightInPoints()}
         *
         * @return row height measured in point size
         * @see NPOI.XSSF.usermodel.XSSFSheet#GetDefaultRowHeightInPoints()
         */
        public float HeightInPoints
        {
            get
            {
                if (this._row.IsSetHt())
                {
                    return (float)this._row.ht;
                }
                return _sheet.DefaultRowHeightInPoints;
            }
            set 
            {
                this.Height = (short)(value == -1 ? -1 : (value * 20));
            }
        }



        /**
         * Gets the number of defined cells (NOT number of cells in the actual row!).
         * That is to say if only columns 0,4,5 have values then there would be 3.
         *
         * @return int representing the number of defined cells in the row.
         */
        public int PhysicalNumberOfCells
        {
            get
            {
                return _cells.Count;
            }
        }

        /**
         * Get row number this row represents
         *
         * @return the row number (0 based)
         */
        public int RowNum
        {
            get
            {
                return (int)_row.r-1;
            }
            set 
            {
                int maxrow = SpreadsheetVersion.EXCEL2007.LastRowIndex;
                if (value < 0 || value > maxrow)
                {
                    throw new ArgumentException("Invalid row number (" + value
                            + ") outside allowable range (0.." + maxrow + ")");
                }
                _row.r = (uint)(value+1);
            }
        }


        /**
         * Get whether or not to display this row with 0 height
         *
         * @return - height is zero or not.
         */
        public bool ZeroHeight
        {
            get
            {
                return (bool)this._row.hidden;
            }
            set 
            {
                this._row.hidden = value;                
            }
        }


        /**
         * Is this row formatted? Most aren't, but some rows
         *  do have whole-row styles. For those that do, you
         *  can get the formatting from {@link #GetRowStyle()}
         */
        public bool IsFormatted
        {
            get
            {
                return _row.IsSetS();
            }
        }
        /**
         * Returns the whole-row cell style. Most rows won't
         *  have one of these, so will return null. Call
         *  {@link #isFormatted()} to check first.
         */
        public ICellStyle RowStyle
        {
            get
            {
                if (!IsFormatted) return null;

                StylesTable stylesSource = ((XSSFWorkbook)Sheet.Workbook).GetStylesSource();
                if (stylesSource.NumCellStyles > 0)
                {
                    return stylesSource.GetStyleAt((int)_row.s);
                }
                else
                {
                    return null;
                }
            }
            set 
            {
                if (value == null)
                {
                    if (_row.IsSetS())
                    {
                        _row.UnsetS();
                        _row.UnsetCustomFormat();
                    }
                }
                else
                {
                    StylesTable styleSource = ((XSSFWorkbook)Sheet.Workbook).GetStylesSource();

                    XSSFCellStyle xStyle = (XSSFCellStyle)value;
                    xStyle.VerifyBelongsToStylesSource(styleSource);

                    long idx = styleSource.PutStyle(xStyle);
                    _row.s = (uint)idx;
                    _row.customFormat = (true);
                }
            }
        }

        /**
         * Applies a whole-row cell styling to the row.
         * If the value is null then the style information is Removed,
         *  causing the cell to used the default workbook style.
         */
        public void SetRowStyle(ICellStyle style)
        {

        }

        /**
         * Remove the Cell from this row.
         *
         * @param cell the cell to remove
         */
        public void RemoveCell(ICell cell)
        {
            if (cell.Row != this)
            {
                throw new ArgumentException("Specified cell does not belong to this row");
            }

            XSSFCell xcell = (XSSFCell)cell;
            if (xcell.IsPartOfArrayFormulaGroup)
            {
                xcell.NotifyArrayFormulaChanging();
            }
            if (cell.CellType == CellType.Formula)
            {
                ((XSSFWorkbook)_sheet.Workbook).OnDeleteFormula(xcell);
            }
            _cells.Remove(cell.ColumnIndex);
        }

        /**
         * Returns the underlying CT_Row xml bean Containing all cell defInitions in this row
         *
         * @return the underlying CT_Row xml bean
         */

        public CT_Row GetCTRow()
        {
            return _row;
        }

        /**
         * Fired when the document is written to an output stream.
         *
         * @see NPOI.XSSF.usermodel.XSSFSheet#Write(java.io.OutputStream) ()
         */
        internal void OnDocumentWrite()
        {
            // check if cells in the CT_Row are ordered
            bool isOrdered = true;
            if (_row.SizeOfCArray() != _cells.Count) isOrdered = false;
            else
            {
                int i = 0;
                foreach (XSSFCell cell in _cells.Values)
                {
                    CT_Cell c1 = cell.GetCTCell();
                    CT_Cell c2 = _row.GetCArray(i++);

                    String r1 = c1.r;
                    String r2 = c2.r;
                    if (!(r1 == null ? r2 == null : r1.Equals(r2)))
                    {
                        isOrdered = false;
                        break;
                    }
                }
            }

            if (!isOrdered)
            {
                CT_Cell[] cArray = new CT_Cell[_cells.Count];
                int i = 0;
                foreach (XSSFCell c in _cells.Values)
                {
                    cArray[i++] = c.GetCTCell();
                }
                _row.SetCArray(cArray);
            }
        }

        /**
         * @return formatted xml representation of this row
         */

        public override String ToString()
        {
            return _row.ToString();
        }

        /**
         * update cell references when Shifting rows
         *
         * @param n the number of rows to move
         */
        internal void Shift(int n)
        {
            int rownum = RowNum + n;
            CalculationChain calcChain = ((XSSFWorkbook)_sheet.Workbook).GetCalculationChain();
            int sheetId = (int)_sheet.sheet.sheetId;
            String msg = "Row[rownum=" + RowNum + "] contains cell(s) included in a multi-cell array formula. " +
                    "You cannot change part of an array.";
            foreach (ICell c in this)
            {
                XSSFCell cell = (XSSFCell)c;
                if (cell.IsPartOfArrayFormulaGroup)
                {
                    cell.NotifyArrayFormulaChanging(msg);
                }

                //remove the reference in the calculation chain
                if (calcChain != null) 
                    calcChain.RemoveItem(sheetId, cell.GetReference());

                CT_Cell CT_Cell = cell.GetCTCell();
                String r = new CellReference(rownum, cell.ColumnIndex).FormatAsString();
                CT_Cell.r = r;
            }
            RowNum = rownum;
        }


        /**
         * Copy the cells from srcRow to this row
         * If this row is not a blank row, this will merge the two rows, overwriting
         * the cells in this row with the cells in srcRow
         * If srcRow is null, overwrite cells in destination row with blank values, styles, etc per cell copy policy
         * srcRow may be from a different sheet in the same workbook
         * @param srcRow the rows to copy from
         * @param policy the policy to determine what gets copied
         */

        public void CopyRowFrom(IRow srcRow, CellCopyPolicy policy)
        {
            if (srcRow == null)
            {
                // srcRow is blank. Overwrite cells with blank values, blank styles, etc per cell copy policy
                foreach (ICell destCell in this)
                {
                    XSSFCell srcCell = null;
                    // FIXME: remove type casting when copyCellFrom(Cell, CellCopyPolicy) is added to Cell interface
                    ((XSSFCell)destCell).CopyCellFrom(srcCell, policy);
                }
                if (policy.IsCopyMergedRegions)
                {
                    // Remove MergedRegions in dest row
                    int destRowNum = RowNum;
                    int index = 0;
                    HashSet<int> indices = new HashSet<int>();
                    foreach (CellRangeAddress destRegion in Sheet.MergedRegions)
                    {
                        if (destRowNum == destRegion.FirstRow && destRowNum == destRegion.LastRow)
                        {
                            indices.Add(index);
                        }
                        index++;
                    }
                    (Sheet as XSSFSheet).RemoveMergedRegions(indices.ToList());
                }
                if (policy.IsCopyRowHeight)
                {
                    // clear row height
                    Height = ((short)-1);
                }
            }
            else
            {
                foreach (ICell c in srcRow)
                {
                    XSSFCell srcCell = (XSSFCell)c;
                    XSSFCell destCell = CreateCell(srcCell.ColumnIndex, srcCell.CellType) as XSSFCell;
                    destCell.CopyCellFrom(srcCell, policy);
                }
                XSSFRowShifter rowShifter = new XSSFRowShifter(_sheet);
                int sheetIndex = _sheet.Workbook.GetSheetIndex(_sheet);
                String sheetName = _sheet.Workbook.GetSheetName(sheetIndex);
                int srcRowNum = srcRow.RowNum;
                int destRowNum = RowNum;
                int rowDifference = destRowNum - srcRowNum;
                FormulaShifter shifter = FormulaShifter.CreateForRowCopy(sheetIndex, sheetName, srcRowNum, srcRowNum, rowDifference, SpreadsheetVersion.EXCEL2007);
                rowShifter.UpdateRowFormulas(this, shifter);
                // Copy merged regions that are fully contained on the row
                // FIXME: is this something that rowShifter could be doing?
                if (policy.IsCopyMergedRegions)
                {
                    foreach (CellRangeAddress srcRegion in srcRow.Sheet.MergedRegions)
                    {
                        if (srcRowNum == srcRegion.FirstRow && srcRowNum == srcRegion.LastRow)
                        {
                            CellRangeAddress destRegion = srcRegion.Copy();
                            destRegion.FirstRow = (destRowNum);
                            destRegion.LastRow = (destRowNum);
                            Sheet.AddMergedRegion(destRegion);
                        }
                    }
                }
                if (policy.IsCopyRowHeight)
                {
                    Height = (srcRow.Height);
                }
            }
        }



        #region IRow Members

        public List<ICell> Cells
        {
            get {
                List<ICell> cells = new List<ICell>();
                foreach (ICell cell in _cells.Values)
                {
                    cells.Add(cell);
                }
                return cells; }
        }

        public void MoveCell(ICell cell, int newColumn)
        {
            throw new NotImplementedException();
        }

        public IRow CopyRowTo(int targetIndex)
        {
            return this.Sheet.CopyRow(this.RowNum, targetIndex);
        }

        public ICell CopyCell(int sourceIndex, int targetIndex)
        {
            return CellUtil.CopyCell(this, sourceIndex, targetIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool HasCustomHeight()
        {
            throw new NotImplementedException();
        }

        public int OutlineLevel
        {
            get
            {
                return _row.outlineLevel;
            }
            set
            {
                _row.outlineLevel = (byte)value;
            }
        }

        public bool? Hidden
        {
            get
            {
                return _row.hidden;
            }

            set
            {
                _row.hidden = value ?? false;
            }
        }

        public bool? Collapsed
        {
            get
            {
                return _row.collapsed;
            }

            set
            {
                _row.collapsed = value ?? false;
            }
        }
        #endregion
    }

}