using System;
using System.Diagnostics;
using System.Threading;

namespace ParallelVectorProcessing
{
    public class Worker
    {
        // Тип нагрузки
        public enum WorkloadType
        {
            Uniform,        // равномерная: K фиксирован
            IndexDependent  // нагрузка зависит от i (например, inner loop up to i % (K+1) или i)
        }

        // Прямой рабочий метод для одного элемента: выполняет "обработку" a[i] -> b[i]
        // с параметром K (число повторов внутренней операции)
        public static void ProcessElement(double[] a, double[] b, int i, int K, WorkloadType workload, ref long operations)
        {
            double sum = 0.0;

            if (workload == WorkloadType.Uniform)
            {
                // Простая/усложнённая обработка: K повторов
                for (int j = 0; j < K; j++)
                {
                    sum += Math.Pow(a[i], 1.789);
                    Interlocked.Increment(ref operations); // считаем внутренние операции
                }
            }
            else
            {
                // Нагрузка зависит от индекса i: сделаем j < (i % max(1,K)) + 1  OR j < i for heavier skew
                int limit = Math.Min(i + 1, Math.Max(1, K)); // ограничим чтобы не сделать слишком тяжело
                for (int j = 0; j < limit; j++)
                {
                    sum += Math.Pow(a[i], 1.789);
                    Interlocked.Increment(ref operations);
                }
            }

            b[i] = sum;
        }

        // ------------------------------
        // Последовательная обработка
        // ------------------------------
        // Возвращает (timeMs, operations)
        public static (double, long) SequentialProcess(double[] a, double[] b, int K, WorkloadType workload)
        {
            int N = a.Length;
            long operations = 0;
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < N; i++)
            {
                ProcessElement(a, b, i, K, workload, ref operations);
            }

            sw.Stop();
            return (sw.Elapsed.TotalMilliseconds, operations);
        }

        // ------------------------------
        // Параллельная обработка: разбиение по диапазону
        // ------------------------------
        public static (double, long) ParallelRangeProcess(double[] a, double[] b, int K, int nThreads, WorkloadType workload)
        {
            int N = a.Length;
            long operations = 0;
            Thread[] threads = new Thread[nThreads];

            // Подготовим параметры для потоков
            int baseCount = N / nThreads;
            int remainder = N % nThreads;

            Stopwatch sw = Stopwatch.StartNew();

            int start = 0;
            for (int t = 0; t < nThreads; t++)
            {
                int chunk = baseCount + (t < remainder ? 1 : 0);
                int localStart = start;
                int localEnd = start + chunk - 1;
                start += chunk;

                // Since we cannot use pointers easily in safe managed code, we use closure capturing (create local copy of required variables)
                int ls = localStart;
                int le = localEnd;
                threads[t] = new Thread(() =>
                {
                    for (int i = ls; i <= le; i++)
                    {
                        ProcessElement(a, b, i, K, workload, ref operations);
                    }
                });
                threads[t].Start();
            }

            // Join
            foreach (var thr in threads)
            {
                if (thr != null)
                    thr.Join();
            }

            sw.Stop();
            return (sw.Elapsed.TotalMilliseconds, operations);
        }

        // ------------------------------
        // Параллельная обработка: круговая (circular) декомпозиция
        // ------------------------------
        public static (double, long) ParallelCircularProcess(double[] a, double[] b, int K, int nThreads, WorkloadType workload)
        {
            int N = a.Length;
            long operations = 0;
            Thread[] threads = new Thread[nThreads];

            Stopwatch sw = Stopwatch.StartNew();

            for (int t = 0; t < nThreads; t++)
            {
                int threadIndex = t;
                threads[t] = new Thread(() =>
                {
                    for (int i = threadIndex; i < N; i += nThreads)
                    {
                        ProcessElement(a, b, i, K, workload, ref operations);
                    }
                });
                threads[t].Start();
            }

            // Join
            foreach (var thr in threads)
            {
                if (thr != null)
                    thr.Join();
            }

            sw.Stop();
            return (sw.Elapsed.TotalMilliseconds, operations);
        }
    }
}