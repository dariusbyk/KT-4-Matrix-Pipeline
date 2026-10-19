using System;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Matrix a = new Matrix(4, 3);
        Matrix b = new Matrix(3, 2);

        int[,] aValues =
        {
            { 1, 2, 3 },
            { 4, 5, 6 },
            { 7, 8, 9 },
            { 2, 1, 3 }
        };

        int[,] bValues =
        {
            { 1, 2 },
            { 3, 4 },
            { 5, 6 }
        };

        for (int i = 0; i < a.Rows; i++)
        {
            for (int j = 0; j < a.Cols; j++)
            {
                a.Data[i, j] = aValues[i, j];
            }
        }

        for (int i = 0; i < b.Rows; i++)
        {
            for (int j = 0; j < b.Cols; j++)
            {
                b.Data[i, j] = bValues[i, j];
            }
        }

        Stopwatch timer = Stopwatch.StartNew();
        Matrix normalResult = Matrix.Multiply(a, b);
        timer.Stop();
        double normalTime = timer.Elapsed.TotalMilliseconds;

        timer.Restart();
        Matrix result = await Matrix.MultiplyAsync(a, b);
        timer.Stop();
        double asyncTime = timer.Elapsed.TotalMilliseconds;

        Console.WriteLine("Результат умножения матриц:");
        for (int i = 0; i < result.Rows; i++)
        {
            for (int j = 0; j < result.Cols; j++)
            {
                Console.Write(result.Data[i, j] + "\t");
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("Обычное умножение: " + normalTime + " мс");
        Console.WriteLine("Асинхронное умножение с конвейером: " + asyncTime + " мс");
    }
}
