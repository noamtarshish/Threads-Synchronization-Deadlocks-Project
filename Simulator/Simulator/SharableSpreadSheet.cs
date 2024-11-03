using System;
using System.Collections.Generic;
using System.Threading;

namespace Simulator
{
    public class SharableSpreadSheet
    {
        private List<List<string>> sheet;
        private int rows;
        private int columns;
        private Semaphore readersLimit;
        private bool isThereReadersLimit;
        private Mutex sheetLock;
        private List<Semaphore> rowMutexes;
        private List<Semaphore> colMutexes;
        private List<int> rowCounters;
        private List<int> colCounters;
        private List<Mutex> rowCountersLock;
        private List<Mutex> colCountersLock;
        private bool doesSheetLock;
        private Mutex atomic;

        public SharableSpreadSheet(int nRows, int nCols, int nUsers = -1)
        {
            if (nRows <= 0 || nCols <= 0)
                throw new ArgumentOutOfRangeException("Rows and columns need to be greater than 0.");

            sheet = new List<List<string>>(nRows);
            for (int i = 0; i < nRows; i++)
            {
                var row = new List<string>(nCols);
                for (int j = 0; j < nCols; j++)
                {
                    row.Add(string.Empty);
                }
                sheet.Add(row);
            }

            rows = nRows;
            columns = nCols;

            SetConcurrentSearchLimit(nUsers);  // Ensure the method is called correctly here

            rowMutexes = new List<Semaphore>(nRows);
            for (int i = 0; i < nRows; i++)
                rowMutexes.Add(new Semaphore(1, 1));

            colMutexes = new List<Semaphore>(nCols);
            for (int i = 0; i < nCols; i++)
                colMutexes.Add(new Semaphore(1, 1));

            rowCounters = new List<int>(new int[nRows]);
            colCounters = new List<int>(new int[nCols]);

            rowCountersLock = new List<Mutex>(nRows);
            for (int i = 0; i < nRows; i++)
                rowCountersLock.Add(new Mutex());

            colCountersLock = new List<Mutex>(nCols);
            for (int i = 0; i < nCols; i++)
                colCountersLock.Add(new Mutex());

            sheetLock = new Mutex();
            atomic = new Mutex();
            doesSheetLock = false;
        }

        private void InRange(int row, int col)
        {
            if (row < 0 || row >= rows || col < 0 || col >= columns)
                throw new ArgumentOutOfRangeException($"Indexes are out of range: [{row}, {col}]");
        }

        public string GetCell(int row, int col)
        {
            InRange(row, col);
            string result;
            while (doesSheetLock) ;

            rowCountersLock[row].WaitOne();
            rowCounters[row]++;
            if (rowCounters[row] == 1)
                rowMutexes[row].WaitOne();
            rowCountersLock[row].ReleaseMutex();

            result = sheet[row][col];

            rowCountersLock[row].WaitOne();
            rowCounters[row]--;
            if (rowCounters[row] == 0)
                rowMutexes[row].Release();
            rowCountersLock[row].ReleaseMutex();

            return result;
        }

        public void SetCell(int row, int col, string str)
        {
            InRange(row, col);
            while (doesSheetLock) ;
            rowMutexes[row].WaitOne();
            sheet[row][col] = str;
            rowMutexes[row].Release();
        }

        public Tuple<int, int> SearchString(string str)
        {
            Tuple<int, int> answer = new Tuple<int, int>(-1, -1);
            bool found = false;
            while (doesSheetLock) ;

            if (isThereReadersLimit)
                readersLimit.WaitOne();

            for (int i = 0; i < rows; i++)
            {
                rowCountersLock[i].WaitOne();
                rowCounters[i]++;
                if (rowCounters[i] == 1)
                    rowMutexes[i].WaitOne();
                rowCountersLock[i].ReleaseMutex();

                for (int j = 0; j < columns; j++)
                {
                    if (sheet[i][j] == str)
                    {
                        answer = new Tuple<int, int>(i, j);
                        found = true;
                        break;
                    }
                }

                rowCountersLock[i].WaitOne();
                rowCounters[i]--;
                if (rowCounters[i] == 0)
                    rowMutexes[i].Release();
                rowCountersLock[i].ReleaseMutex();

                if (found)
                    break;
            }

            if (isThereReadersLimit)
                readersLimit.Release();

            return answer;
        }

        public void ExchangeRows(int row1, int row2)
        {
            if (row1 != row2)
            {
                InRange(row1, 0);
                InRange(row2, 0);

                while (doesSheetLock) ;
                atomic.WaitOne();

                rowMutexes[row1].WaitOne();
                rowMutexes[row2].WaitOne();
                atomic.ReleaseMutex();

                for (int i = 0; i < columns; i++)
                {
                    string temp = sheet[row1][i];
                    sheet[row1][i] = sheet[row2][i];
                    sheet[row2][i] = temp;
                }

                rowMutexes[row1].Release();
                rowMutexes[row2].Release();
            }
        }

        public void ExchangeCols(int col1, int col2)
        {
            if (col1 != col2)
            {
                InRange(0, col1);
                InRange(0, col2);

                while (doesSheetLock) ;
                atomic.WaitOne();

                colMutexes[col1].WaitOne();
                colMutexes[col2].WaitOne();
                atomic.ReleaseMutex();

                for (int i = 0; i < rows; i++)
                {
                    rowMutexes[i].WaitOne();
                    string temp = sheet[i][col1];
                    sheet[i][col1] = sheet[i][col2];
                    sheet[i][col2] = temp;
                    rowMutexes[i].Release();
                }

                colMutexes[col1].Release();
                colMutexes[col2].Release();
            }
        }

        public int SearchInRow(int row, string str)
        {
            InRange(row, 0);
            int answer = -1;
            while (doesSheetLock) ;

            if (isThereReadersLimit)
                readersLimit.WaitOne();

            rowCountersLock[row].WaitOne();
            rowCounters[row]++;
            if (rowCounters[row] == 1)
                rowMutexes[row].WaitOne();
            rowCountersLock[row].ReleaseMutex();

            for (int i = 0; i < columns; i++)
            {
                if (sheet[row][i] == str)
                {
                    answer = i;
                    break;
                }
            }

            rowCountersLock[row].WaitOne();
            rowCounters[row]--;
            if (rowCounters[row] == 0)
                rowMutexes[row].Release();
            rowCountersLock[row].ReleaseMutex();

            if (isThereReadersLimit)
                readersLimit.Release();

            return answer;
        }

        public int SearchInCol(int col, string str)
        {
            InRange(0, col);
            int answer = -1;
            while (doesSheetLock) ;

            if (isThereReadersLimit)
                readersLimit.WaitOne();

            colCountersLock[col].WaitOne();
            colCounters[col]++;
            if (colCounters[col] == 1)
                colMutexes[col].WaitOne();
            colCountersLock[col].ReleaseMutex();

            for (int i = 0; i < rows; i++)
            {
                rowCountersLock[i].WaitOne();
                rowCounters[i]++;
                if (rowCounters[i] == 1)
                    rowMutexes[i].WaitOne();
                rowCountersLock[i].ReleaseMutex();

                if (sheet[i][col] == str)
                {
                    answer = i;
                    rowCountersLock[i].WaitOne();
                    rowCounters[i]--;
                    if (rowCounters[i] == 0)
                        rowMutexes[i].Release();
                    rowCountersLock[i].ReleaseMutex();
                    break;
                }

                rowCountersLock[i].WaitOne();
                rowCounters[i]--;
                if (rowCounters[i] == 0)
                    rowMutexes[i].Release();
                rowCountersLock[i].ReleaseMutex();
            }

            colCountersLock[col].WaitOne();
            colCounters[col]--;
            if (colCounters[col] == 0)
                colMutexes[col].Release();
            colCountersLock[col].ReleaseMutex();

            if (isThereReadersLimit)
                readersLimit.Release();

            return answer;
        }

        public Tuple<int, int> SearchInRange(int col1, int col2, int row1, int row2, string str)
        {
            InRange(row1, col1);
            InRange(row2, col2);

            if (col1 > col2 || row1 > row2)
                throw new ArgumentException("Invalid range.");

            Tuple<int, int> answer = new Tuple<int, int>(-1, -1);
            bool found = false;
            while (doesSheetLock) ;

            if (isThereReadersLimit)
                readersLimit.WaitOne();

            for (int i = row1; i <= row2; i++)
            {
                rowCountersLock[i].WaitOne();
                rowCounters[i]++;
                if (rowCounters[i] == 1)
                    rowMutexes[i].WaitOne();
                rowCountersLock[i].ReleaseMutex();

                for (int j = col1; j <= col2; j++)
                {
                    if (sheet[i][j] == str)
                    {
                        answer = new Tuple<int, int>(i, j);
                        found = true;
                        break;
                    }
                }

                rowCountersLock[i].WaitOne();
                rowCounters[i]--;
                if (rowCounters[i] == 0)
                    rowMutexes[i].Release();
                rowCountersLock[i].ReleaseMutex();

                if (found)
                    break;
            }

            if (isThereReadersLimit)
                readersLimit.Release();

            return answer;
        }

        public void AddRow(int row1)
        {
            if (row1 < 0 || row1 >= rows)
                throw new ArgumentOutOfRangeException("The row index is out of range.");

            sheetLock.WaitOne();
            doesSheetLock = true;

            var newSheet = new List<List<string>>();

            for (int i = 0; i < rows + 1; i++)
            {
                newSheet.Add(new List<string>(new string[columns]));
            }

            for (int i = 0; i <= row1; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    newSheet[i][j] = sheet[i][j];
                }
            }

            for (int i = row1 + 1; i < rows + 1; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    newSheet[i][j] = sheet[i - 1][j];
                }
            }

            sheet = newSheet;
            rows++;

            rowMutexes.Insert(row1 + 1, new Semaphore(1, 1));
            rowCounters.Insert(row1 + 1, 0);
            rowCountersLock.Insert(row1 + 1, new Mutex());

            doesSheetLock = false;
            sheetLock.ReleaseMutex();
        }

        public void AddCol(int col1)
        {
            if (col1 < 0 || col1 >= columns)
                throw new ArgumentOutOfRangeException("The column index is out of range.");

            sheetLock.WaitOne();
            doesSheetLock = true;

            var newSheet = new List<List<string>>(rows);
            for (int i = 0; i < rows; i++)
            {
                newSheet.Add(new List<string>(new string[columns + 1]));
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j <= col1; j++)
                {
                    newSheet[i][j] = sheet[i][j];
                }

                for (int j = col1 + 1; j < columns + 1; j++)
                {
                    newSheet[i][j] = sheet[i][j - 1];
                }
            }

            sheet = newSheet;
            columns++;

            colMutexes.Insert(col1 + 1, new Semaphore(1, 1));
            colCounters.Insert(col1 + 1, 0);
            colCountersLock.Insert(col1 + 1, new Mutex());

            doesSheetLock = false;
            sheetLock.ReleaseMutex();
        }



        public Tuple<int, int>[] FindAll(string str, bool caseSensitive)
        {
            var results = new List<Tuple<int, int>>();
            while (doesSheetLock) ;

            for (int i = 0; i < rows; i++)
            {
                rowCountersLock[i].WaitOne();
                rowCounters[i]++;
                if (rowCounters[i] == 1)
                    rowMutexes[i].WaitOne();
                rowCountersLock[i].ReleaseMutex();

                for (int j = 0; j < columns; j++)
                {
                    if ((caseSensitive && sheet[i][j] == str) || (!caseSensitive && string.Equals(sheet[i][j], str, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new Tuple<int, int>(i, j));
                    }
                }

                rowCountersLock[i].WaitOne();
                rowCounters[i]--;
                if (rowCounters[i] == 0)
                    rowMutexes[i].Release();
                rowCountersLock[i].ReleaseMutex();
            }

            return results.ToArray();
        }

        public void SetAll(string oldStr, string newStr, bool caseSensitive)
        {
            if (oldStr == null || newStr == null)
                throw new ArgumentNullException("One of the strings is null.");

            while (doesSheetLock) ;

            for (int i = 0; i < rows; i++)
            {
                rowMutexes[i].WaitOne();

                for (int j = 0; j < columns; j++)
                {
                    if ((caseSensitive && sheet[i][j] == oldStr) || (!caseSensitive && string.Equals(sheet[i][j], oldStr, StringComparison.OrdinalIgnoreCase)))
                    {
                        sheet[i][j] = newStr;
                    }
                }

                rowMutexes[i].Release();
            }
        }

        public Tuple<int, int> GetSize()
        {
            return new Tuple<int, int>(rows, columns);
        }

        public void SetConcurrentSearchLimit(int nUsers)
        {
            if (nUsers == -1)
            {
                isThereReadersLimit = false;
            }
            else
            {
                isThereReadersLimit = true;
                readersLimit = new Semaphore(nUsers, nUsers);
            }
        }

        public void Print()
        {
            int[] maxLengths = new int[columns];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    if (sheet[i][j].Length > maxLengths[j])
                    {
                        maxLengths[j] = sheet[i][j].Length;
                    }
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    Console.Write(sheet[i][j].PadRight(maxLengths[j] + 2));
                }
                Console.WriteLine();
            }
        }

    }
}
