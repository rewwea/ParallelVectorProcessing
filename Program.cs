using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ParallelVectorProcessing
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("ParallelVectorProcessing - Lab1");
            Console.WriteLine("Starting experiments...");

            // Параметры экспериментов (можно менять)
            int[] Ns = new int[] { 10, 100, 1000, 100000 };           // размеры вектора
            int[] Ms = new int[] { 2, 3, 4, 5, 10 };                  // числа потоков для тестов
            int[] Ks = new int[] { 1, 10, 100 };                      // K - сложность (вложенный цикл): 1=простая, 10/100 = сложнее
            int runsPerSetting = 3;                                   // количество прогонов (возьмём медиану/среднее)

            // Создать папку для результатов
            Directory.CreateDirectory("results");
            string csvPath = Path.Combine("results", "results.csv");

            using (var sw = new StreamWriter(csvPath))
            {
                // Заголовок CSV
                sw.WriteLine("Mode,N,M,K,PartitionType,TimeMs,Operations,Run");

                // Итерации: последовательная обработка (для сравнения) для всех N, для каждого K
                foreach (int N in Ns)
                {
                    foreach (int K in Ks)
                    {
                        for (int run = 1; run <= runsPerSetting; run++)
                        {
                            // Порождаем данные
                            double[] a = Utils.GenerateVector(N);
                            double[] b = new double[N];

                            // последовательная обработка (равномерная нагрузка)
                            var (timeMs, ops) = Worker.SequentialProcess(a, b, K, Worker.WorkloadType.Uniform);
                            Console.WriteLine($"Seq N={N} K={K} run={run} time={timeMs}ms ops={ops}");
                            sw.WriteLine($"Sequential,{N},1,{K},None,{timeMs},{ops},{run}");
                        }
                    }
                }

                // Многопоточные прогоны: перебираем комбинации N, M, K
                foreach (int N in Ns)
                {
                    foreach (int M in Ms)
                    {
                        foreach (int K in Ks)
                        {
                            // 1) Разделение по диапазону, равномерная нагрузка (Uniform)
                            for (int run = 1; run <= runsPerSetting; run++)
                            {
                                double[] a = Utils.GenerateVector(N);
                                double[] b = new double[N];
                                var (timeMs, ops) = Worker.ParallelRangeProcess(a, b, K, M, Worker.WorkloadType.Uniform);
                                Console.WriteLine($"Range  N={N} M={M} K={K} run={run} time={timeMs}ms ops={ops}");
                                sw.WriteLine($"Parallel,{N},{M},{K},Range-Uniform,{timeMs},{ops},{run}");
                            }

                            // 2) Круговая декомпозиция (circular), равномерная нагрузка
                            for (int run = 1; run <= runsPerSetting; run++)
                            {
                                double[] a = Utils.GenerateVector(N);
                                double[] b = new double[N];
                                var (timeMs, ops) = Worker.ParallelCircularProcess(a, b, K, M, Worker.WorkloadType.Uniform);
                                Console.WriteLine($"Circular  N={N} M={M} K={K} run={run} time={timeMs}ms ops={ops}");
                                sw.WriteLine($"Parallel,{N},{M},{K},Circular-Uniform,{timeMs},{ops},{run}");
                            }

                            // 3) Разделение по диапазону, неравномерная нагрузка (workload ~ element-dependent)
                            for (int run = 1; run <= runsPerSetting; run++)
                            {
                                double[] a = Utils.GenerateVector(N);
                                double[] b = new double[N];
                                var (timeMs, ops) = Worker.ParallelRangeProcess(a, b, K, M, Worker.WorkloadType.IndexDependent);
                                Console.WriteLine($"Range-Ind N={N} M={M} K={K} run={run} time={timeMs}ms ops={ops}");
                                sw.WriteLine($"Parallel,{N},{M},{K},Range-IndexDep,{timeMs},{ops},{run}");
                            }

                            // 4) Круговая декомпозиция, неравномерная нагрузка
                            for (int run = 1; run <= runsPerSetting; run++)
                            {
                                double[] a = Utils.GenerateVector(N);
                                double[] b = new double[N];
                                var (timeMs, ops) = Worker.ParallelCircularProcess(a, b, K, M, Worker.WorkloadType.IndexDependent);
                                Console.WriteLine($"Circular-Ind N={N} M={M} K={K} run={run} time={timeMs}ms ops={ops}");
                                sw.WriteLine($"Parallel,{N},{M},{K},Circular-IndexDep,{timeMs},{ops},{run}");
                            }
                        }
                    }
                }

                Console.WriteLine($"Experiments finished. Results saved to {csvPath}");
            }
        }
    }
}