using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Threading;

namespace GcPlayground.ConsoleApp.Scenarios
{
    /// <summary>
    /// Демонстрирует поведение Large Object Heap (LOH).
    /// 
    /// LOH - это отдельная куча для больших объектов (> 85KB по умолчанию).
    /// Особенности LOH:
    /// - Объекты не перемещаются (нет компактизации)
    /// - Может привести к фрагментации памяти
    /// - Требует явной компактизации (GCSettings.LargeObjectHeapCompactionMode)
    /// - Влияет на производительность при частом выделении/освобождении
    /// </summary>
    public class LohScenario
    {
        /// <summary>
        /// Тест выделения памяти в LOH.
        /// 
        /// Что происходит:
        /// 1. Создаем объекты размером 100KB (попадают в LOH, т.к. > 85KB)
        /// 2. Объекты не сохраняются, сразу становятся мусором
        /// 3. GC собирает их, но память может остаться фрагментированной
        /// 4. Наблюдаем как часто происходят сборки LOH
        /// 
        /// Что смотреть:
        /// - Gen2 сборки происходят чаще (LOH собирается вместе с Gen2)
        /// - Время выполнения может быть нестабильным (LOH сборка дороже)
        /// - Память может расти из-за фрагментации
        /// </summary>
        public static void RunLohAllocationTest()
        {
            Console.WriteLine("=== LOH Allocations Test ===");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 0;

            while (true)
            {
                // Создаем объект размером 100KB - это попадает в LOH (Large Object Heap)
                // Порог LOH по умолчанию 85KB, поэтому 100KB гарантированно в LOH
                var arr = new byte[100_000];
                iterations++;

                // Каждые 100 итераций выводим статистику
                if (iterations % 100 == 0)
                {
                    sw.Stop();
                    Console.WriteLine($"Iterations: {iterations}, Time: {sw.ElapsedMilliseconds}ms, " +
                        $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}, " +
                        $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                    sw.Restart();
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Тест фрагментации LOH.
        /// 
        /// Что происходит:
        /// 1. Добавляем объекты в список (они остаются в памяти)
        /// 2. Удаляем старые объекты (создаем "дыры" в памяти)
        /// 3. Новые объекты не могут использовать эти дыры (нет компактизации)
        /// 4. Память растет, хотя мы удаляем объекты
        /// 
        /// Что смотреть:
        /// - Память растет несмотря на удаление объектов
        /// - Фрагментация видна по разнице между объемом объектов и общей памятью
        /// - Gen2 сборки не помогают (LOH не компактизируется автоматически)
        /// </summary>
        public static void RunLohFragmentationTest()
        {
            Console.WriteLine("=== LOH Fragmentation Test ===");
            // Список для управления объектами LOH
            var list = new List<byte[]>();

            while (true)
            {
                // Добавляем объект 100KB в LOH
                list.Add(new byte[100_000]);

                // Удаляем старые объекты, чтобы создать фрагментацию
                // Когда список превышает 1000 объектов, удаляем первый
                if (list.Count > 1000)
                    list.RemoveAt(0);

                // Каждые 100 объектов выводим статистику
                if (list.Count % 100 == 0)
                {
                    // Обратите внимание: память не уменьшается, хотя мы удаляем объекты
                    // Это фрагментация LOH
                    Console.WriteLine($"List size: {list.Count}, " +
                        $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}, " +
                        $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                }
            }
        }

        /// <summary>
        /// Тест компактизации LOH.
        /// 
        /// Что происходит:
        /// 1. Создаем фрагментацию LOH (как в предыдущем тесте)
        /// 2. Периодически вызываем явную компактизацию LOH
        /// 3. Компактизация перемещает объекты, заполняя "дыры"
        /// 4. Наблюдаем эффект компактизации на использование памяти
        /// 
        /// Что смотреть:
        /// - После компактизации память может уменьшиться
        /// - Компактизация дорогая операция (может вызвать паузу)
        /// - Компактизация помогает, но не решает проблему полностью
        /// 
        /// Важно:
        /// - GCLargeObjectHeapCompactionMode.CompactOnce - компактизирует один раз
        /// - Нужно вызывать GC.Collect() чтобы применить компактизацию
        /// </summary>
        public static void RunLohCompactionTest()
        {
            Console.WriteLine("=== LOH Compaction Test ===");
            var list = new List<byte[]>();
            int iteration = 0;

            while (true)
            {
                // Добавляем объект в LOH
                list.Add(new byte[100_000]);

                // Удаляем старые объекты для создания фрагментации
                if (list.Count > 1000)
                    list.RemoveAt(0);

                iteration++;

                // Каждые 5000 итераций выполняем компактизацию LOH
                if (iteration % 5000 == 0)
                {
                    Console.WriteLine("Triggering LOH compaction...");
                    // Устанавливаем режим компактизации LOH
                    // CompactOnce - компактизирует один раз при следующей сборке Gen2
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    // Принудительно вызываем сборку мусора для применения компактизации
                    GC.Collect();
                    Console.WriteLine($"Compaction done. Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                }

                // Каждые 500 итераций выводим статистику
                if (iteration % 500 == 0)
                {
                    Console.WriteLine($"Iteration: {iteration}, List size: {list.Count}, " +
                        $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                }
            }
        }
    }
}
