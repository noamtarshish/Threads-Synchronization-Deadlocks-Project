using System;
using System.Collections.Generic;
using System.Threading;

namespace Simulator
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 5)
            {
                Console.WriteLine("The application was not given enough arguments.");
                return;
            }

            if (!int.TryParse(args[0], out int rows) ||
                !int.TryParse(args[1], out int columns) ||
                !int.TryParse(args[2], out int nThreads) ||
                !int.TryParse(args[3], out int nOperations) ||
                !int.TryParse(args[4], out int mssleep))
            {
                Console.WriteLine("All arguments must be numbers.");
                return;
            }

            if (nThreads < 0 || nOperations < 0 || mssleep < 0)
            {
                Console.WriteLine("Number of threads, operations, and sleep duration must be positive numbers.");
                return;
            }

            int nUsers = -1;
            if (args.Length == 6 && !int.TryParse(args[5], out nUsers))
            {
                Console.WriteLine("Invalid user limit argument.");
                return;
            }

            SharableSpreadSheet sheet = new SharableSpreadSheet(rows, columns, nUsers);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < columns; j++)
                {
                    sheet.SetCell(i, j, $"Test{i}{j}");
                }
            }

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < nThreads; i++)
            {
                Thread thread = new Thread(() => Simulation(nOperations, rows, columns, sheet, mssleep));
                thread.Name = $"Thread {i}";
                threads.Add(thread);
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Console.WriteLine("Simulation completed.");
        }

        public static void Simulation(int nOperations, int rows, int columns, SharableSpreadSheet sheet, int mssleep)
        {
            Random rnd = new Random();

            for (int i = 0; i < nOperations; i++)
            {
                int num = rnd.Next(13);
                try
                {
                    switch (num)
                    {
                        case 0:
                            GetCellOperation(rows, columns, sheet, rnd);
                            break;
                        case 1:
                            SetCellOperation(rows, columns, sheet, rnd);
                            break;
                        case 2:
                            SearchStringOperation(rows, columns, sheet, rnd);
                            break;
                        case 3:
                            ExchangeRowsOperation(rows, sheet, rnd);
                            break;
                        case 4:
                            ExchangeColsOperation(columns, sheet, rnd);
                            break;
                        case 5:
                            SearchInRowOperation(rows, columns, sheet, rnd);
                            break;
                        case 6:
                            SearchInColOperation(rows, columns, sheet, rnd);
                            break;
                        case 7:
                            SearchInRangeOperation(rows, columns, sheet, rnd);
                            break;
                        case 8:
                            AddRowOperation(rows, sheet, rnd);
                            break;
                        case 9:
                            AddColOperation(columns, sheet, rnd);
                            break;
                        case 10:
                            FindAllOperation(rows, columns, sheet, rnd);
                            break;
                        case 11:
                            SetAllOperation(rows, columns, sheet, rnd);
                            break;
                        case 12:
                            GetSizeOperation(sheet);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"catch: {ex.Message}");
                }
                Thread.Sleep(mssleep);
            }
        }

        private static void GetCellOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            int col = rnd.Next(columns);
            string cell = sheet.GetCell(row, col);
            Console.WriteLine($"{Thread.CurrentThread.Name}: GetCell({row}, {col}) = {cell}");
        }

        private static void SetCellOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            int col = rnd.Next(columns);
            string value = $"Value{row}{col}";
            sheet.SetCell(row, col, value);
            Console.WriteLine($"{Thread.CurrentThread.Name}: SetCell({row}, {col}, {value})");
        }

        private static void SearchStringOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            int col = rnd.Next(columns);
            string searchString = $"Test{row}{col}";
            var result = sheet.SearchString(searchString);
            Console.WriteLine($"{Thread.CurrentThread.Name}: SearchString({searchString}) = ({result.Item1}, {result.Item2})");
        }

        private static void ExchangeRowsOperation(int rows, SharableSpreadSheet sheet, Random rnd)
        {
            int row1 = rnd.Next(rows);
            int row2 = rnd.Next(rows);
            sheet.ExchangeRows(row1, row2);
            Console.WriteLine($"{Thread.CurrentThread.Name}: ExchangeRows({row1}, {row2})");
        }

        private static void ExchangeColsOperation(int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int col1 = rnd.Next(columns);
            int col2 = rnd.Next(columns);
            sheet.ExchangeCols(col1, col2);
            Console.WriteLine($"{Thread.CurrentThread.Name}: ExchangeCols({col1}, {col2})");
        }

        private static void SearchInRowOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            int col = rnd.Next(columns);
            string searchString = $"Test{row}{col}";
            int result = sheet.SearchInRow(row, searchString);
            Console.WriteLine($"{Thread.CurrentThread.Name}: SearchInRow({row}, {searchString}) = {result}");
        }

        private static void SearchInColOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            int col = rnd.Next(columns);
            string searchString = $"Test{row}{col}";
            int result = sheet.SearchInCol(col, searchString);
            Console.WriteLine($"{Thread.CurrentThread.Name}: SearchInCol({col}, {searchString}) = {result}");
        }

        private static void SearchInRangeOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row1 = rnd.Next(rows);
            int row2 = rnd.Next(rows);
            int col1 = rnd.Next(columns);
            int col2 = rnd.Next(columns);
            string searchString = $"Test{rnd.Next(rows)}{rnd.Next(columns)}";
            var result = sheet.SearchInRange(col1, col2, row1, row2, searchString);
            Console.WriteLine($"{Thread.CurrentThread.Name}: SearchInRange({col1}, {col2}, {row1}, {row2}, {searchString}) = ({result.Item1}, {result.Item2})");
        }

        private static void AddRowOperation(int rows, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            sheet.AddRow(row);
            Console.WriteLine($"{Thread.CurrentThread.Name}: AddRow({row})");
        }

        private static void AddColOperation(int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int col = rnd.Next(columns);
            sheet.AddCol(col);
            Console.WriteLine($"{Thread.CurrentThread.Name}: AddCol({col})");
        }

        private static void FindAllOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int row = rnd.Next(rows);
            int col = rnd.Next(columns);
            string searchString = $"Test{row}{col}";
            var results = sheet.FindAll(searchString, true);
            var resultsFormatted = string.Join(", ", results.Select(r => $"({r.Item1}, {r.Item2})"));

            Console.WriteLine($"{Thread.CurrentThread.Name}: FindAll({searchString}, true) = [{resultsFormatted}]");
        }


        private static void SetAllOperation(int rows, int columns, SharableSpreadSheet sheet, Random rnd)
        {
            int oldRow = rnd.Next(rows);
            int oldCol = rnd.Next(columns);
            int newRow = rnd.Next(rows);
            int newCol = rnd.Next(columns);
            string oldString = $"Test{oldRow}{oldCol}";
            string newString = $"New{newRow}{newCol}";
            sheet.SetAll(oldString, newString, false);
            Console.WriteLine($"{Thread.CurrentThread.Name}: SetAll({oldString}, {newString}, false)");
        }

        private static void GetSizeOperation(SharableSpreadSheet sheet)
        {
            var size = sheet.GetSize();
            Console.WriteLine($"{Thread.CurrentThread.Name}: GetSize() = ({size.Item1}, {size.Item2})");
        }
    }
}
