namespace ParallelVectorProcessing
{
    // Заготовка для параметров потока (не используется в текущей реализации,
    // оставлена здесь для расширения)
    public class ThreadParams
    {
        public double[] A;
        public double[] B;
        public int Start;
        public int End;
        public int K;
        public Worker.WorkloadType Workload;
    }

    // Вспомогательная структура, если бы потребовались указатели/ссылки
    public class OperationsRef
    {
        public long Ops;
    }
}