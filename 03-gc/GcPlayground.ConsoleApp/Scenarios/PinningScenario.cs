using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GcPlayground.ConsoleApp.Scenarios
{
    /// <summary>
    /// Демонстрирует эффекты pinning (закрепления) объектов в памяти.
    /// 
    /// Pinning - это когда объект не может быть перемещен GC.
    /// 
    /// Два способа pinning:
    /// 1. fixed statement - краткосрочное закрепление (на время блока)
    /// 2. GCHandle.Pinned - долгосрочное закрепление (до явного освобождения)
    /// 
    /// Проблемы с pinning:
    /// - Pinned объекты не могут быть перемещены при компактизации
    /// - Это может привести к фрагментации памяти
    /// - Может замедлить сборку мусора
    /// - Особенно проблематично в Gen2 (долгоживущие объекты)
    /// </summary>
    public class PinningScenario
    {
        /// <summary>
        /// Тест краткосрочного pinning с использованием fixed.
        /// 
        /// Что происходит:
        /// 1. Создаем объект byte[1000]
        /// 2. Используем fixed для получения указателя на его данные
        /// 3. Pinning действует только внутри блока fixed
        /// 4. После выхода из блока объект может быть перемещен
        /// 5. Повторяем много раз
        /// 
        /// Что смотреть:
        /// - Время выполнения остается стабильным
        /// - Нет заметного замедления (pinning краткосрочный)
        /// - GC может нормально работать между итерациями
        /// 
        /// Когда использовать fixed:
        /// - Когда нужен указатель на управляемый объект для P/Invoke
        /// - Краткосрочно (в пределах одного метода)
        /// - Безопасно для производительности
        /// </summary>
        public static void RunFixedPinningTest()
        {
            Console.WriteLine("=== Fixed Pinning Test ===");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int iterations = 0;

            while (true)
            {
                // Создаем объект
                var arr = new byte[1000];

                // unsafe блок требуется для работы с указателями
                unsafe
                {
                    // fixed закрепляет объект только на время этого блока
                    // После выхода из блока объект может быть перемещен
                    fixed (byte* ptr = arr)
                    {
                        // Используем указатель для доступа к данным
                        *ptr = 42;
                    }
                }

                iterations++;

                // Каждые 100000 итераций выводим статистику
                if (iterations % 100000 == 0)
                {
                    sw.Stop();
                    Console.WriteLine($"Iterations: {iterations}, Time: {sw.ElapsedMilliseconds}ms, " +
                        $"Gen0: {GC.CollectionCount(0)}, Gen1: {GC.CollectionCount(1)}, Gen2: {GC.CollectionCount(2)}");
                    sw.Restart();
                }

                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Тест долгосрочного pinning с использованием GCHandle.
        /// 
        /// Что происходит:
        /// 1. Создаем 10000 объектов и закрепляем их с помощью GCHandle.Pinned
        /// 2. Pinned объекты не могут быть перемещены GC
        /// 3. Повышаем объекты в Gen2
        /// 4. Вызываем GC.Collect() и наблюдаем как долго это занимает
        /// 5. Pinned объекты блокируют компактизацию
        /// 
        /// Что смотреть:
        /// - Время сборки мусора значительно возрастает
        /// - GC не может компактизировать память из-за pinned объектов
        /// - Это может привести к фрагментации памяти
        /// - Каждая сборка Gen2 становится дороже
        /// 
        /// Когда избегать:
        /// - Не закрепляйте объекты надолго
        /// - Не закрепляйте много объектов одновременно
        /// - Освобождайте pinned объекты как можно скорее
        /// </summary>
        public static void RunGcHandlePinningTest()
        {
            Console.WriteLine("=== GCHandle Pinning Test ===");
            var handles = new List<GCHandle>();

            // Создаем и закрепляем объекты
            Console.WriteLine("Pinning 10,000 objects...");
            for (int i = 0; i < 10000; i++)
            {
                var arr = new byte[1000];
                // GCHandle.Pinned закрепляет объект в памяти
                // Объект не может быть перемещен до освобождения handle
                var handle = GCHandle.Alloc(arr, GCHandleType.Pinned);
                handles.Add(handle);

                if (i % 1000 == 0)
                    Console.WriteLine($"Pinned {i} objects");
            }

            // Повышаем pinned объекты в Gen2
            Console.WriteLine("All objects pinned. Triggering GC...");
            Console.WriteLine("Press any key to start...");
            Console.ReadKey();

            // Вызываем сборку мусора несколько раз и измеряем время
            for (int i = 0; i < 10; i++)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                // GC.Collect() вызывает сборку мусора
                // С pinned объектами это становится дороже
                GC.Collect();
                sw.Stop();
                Console.WriteLine($"GC #{i} completed in {sw.ElapsedMilliseconds}ms, " +
                    $"Memory: {GC.GetTotalMemory(false) / 1024 / 1024}MB");
                Thread.Sleep(1000);
            }

            // Освобождаем pinned объекты
            Console.WriteLine("Freeing pinned objects...");
            foreach (var h in handles)
                // Освобождаем handle, позволяя объекту быть перемещенным
                h.Free();

            Console.WriteLine("Done");
        }
    }
}
