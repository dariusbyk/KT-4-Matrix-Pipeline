using System;
using System.Threading.Channels;
using System.Threading.Tasks;

public class RowData
{
    public int RowNumber;
    public int[] Values;

    public RowData(int rowNumber, int[] values)
    {
        RowNumber = rowNumber;
        Values = values;
    }
}

public class Matrix
{
    public int[,] Data { get; }
    public int Rows => Data.GetLength(0);
    public int Cols => Data.GetLength(1);

    public Matrix(int rows, int cols)
    {
        Data = new int[rows, cols];
    }

    // Обычное умножение для сравнения с асинхронным.
    public static Matrix Multiply(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows)
        {
            throw new ArgumentException("Нельзя умножить матрицы с такими размерами.");
        }

        Matrix result = new Matrix(a.Rows, b.Cols);

        for (int i = 0; i < a.Rows; i++)
        {
            for (int j = 0; j < b.Cols; j++)
            {
                for (int k = 0; k < a.Cols; k++)
                {
                    result.Data[i, j] += a.Data[i, k] * b.Data[k, j];
                }
            }
        }

        return result;
    }

    // Асинхронное умножение с конвейером из трёх этапов.
    public static async Task<Matrix> MultiplyAsync(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows)
        {
            throw new ArgumentException("Нельзя умножить матрицы с такими размерами.");
        }

        Matrix result = new Matrix(a.Rows, b.Cols);

        Channel<RowData> preparedRows = Channel.CreateBounded<RowData>(2);
        Channel<RowData> calculatedRows = Channel.CreateBounded<RowData>(2);

        Task preparation = Task.Run(() => PrepareRowsAsync(a, preparedRows.Writer));
        Task calculation = Task.Run(() => CalculateRowsAsync(b, preparedRows.Reader, calculatedRows.Writer));
        Task collection = Task.Run(() => CollectRowsAsync(result, calculatedRows.Reader));

        await Task.WhenAll(preparation, calculation, collection);
        return result;
    }

    // Этап 1. Подготовка строк матрицы A.
    private static async Task PrepareRowsAsync(Matrix a, ChannelWriter<RowData> writer)
    {
        for (int i = 0; i < a.Rows; i++)
        {
            int[] values = new int[a.Cols];

            for (int j = 0; j < a.Cols; j++)
            {
                values[j] = a.Data[i, j];
            }

            await writer.WriteAsync(new RowData(i, values));
        }

        writer.Complete();
    }

    // Этап 2. Вычисление строк произведения матриц.
    private static async Task CalculateRowsAsync(Matrix b, ChannelReader<RowData> reader,
        ChannelWriter<RowData> writer)
    {
        while (await reader.WaitToReadAsync())
        {
            RowData row;
            while (reader.TryRead(out row))
            {
                int[] values = new int[b.Cols];

                for (int j = 0; j < b.Cols; j++)
                {
                    for (int k = 0; k < row.Values.Length; k++)
                    {
                        values[j] += row.Values[k] * b.Data[k, j];
                    }
                }

                await writer.WriteAsync(new RowData(row.RowNumber, values));
            }
        }

        writer.Complete();
    }

    // Этап 3. Запись вычисленных строк в результат.
    private static async Task CollectRowsAsync(Matrix result, ChannelReader<RowData> reader)
    {
        while (await reader.WaitToReadAsync())
        {
            RowData row;
            while (reader.TryRead(out row))
            {
                for (int j = 0; j < row.Values.Length; j++)
                {
                    result.Data[row.RowNumber, j] = row.Values[j];
                }
            }
        }
    }
}
