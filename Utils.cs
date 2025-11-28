using System;

namespace ParallelVectorProcessing
{
    public static class Utils
    {
        private static Random rnd = new Random(12345);

        // Генерирует вектор double длины N (рандомные значения)
        public static double[] GenerateVector(int N)
        {
            double[] a = new double[N];
            for (int i = 0; i < N; i++)
            {
                // небольшие числа, чтобы Math.Pow не переполнял
                a[i] = rnd.NextDouble() * 10.0 + 1.0;
            }
            return a;
        }
    }
}