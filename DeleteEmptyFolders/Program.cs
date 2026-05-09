using System;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace EmptyFoldersCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Программа для поиска и удаления пустых папок");
            Console.WriteLine("=============================================\n");

            string searchPath;

            if (args.Length > 0)
            {
                searchPath = args[0];
            }
            else
            {
                Console.Write("Введите путь для поиска пустых папок: ");
                searchPath = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(searchPath))
                {
                    searchPath = Directory.GetCurrentDirectory();
                    Console.WriteLine($"Используется текущая директория: {searchPath}");
                }
            }

            try
            {
                if (!Directory.Exists(searchPath))
                {
                    Console.WriteLine($"Ошибка: Путь '{searchPath}' не существует.");
                    return;
                }

                Console.WriteLine($"\nПоиск пустых папок в: {searchPath}");
                Console.WriteLine("=============================================");

                // Получаем все папки в указанной директории (без рекурсии)
                var directories = Directory.GetDirectories(searchPath);

                if (directories.Length == 0)
                {
                    Console.WriteLine("В указанной директории нет папок.");
                    return;
                }

                Console.WriteLine($"\nНайдено папок для проверки: {directories.Length}");
                Console.WriteLine("=============================================");

                var emptyFolders = new List<string>();

                // Проверяем каждую папку
                foreach (var directory in directories)
                {
                    try
                    {
                        // Получаем информацию о папке
                        var dirInfo = new DirectoryInfo(directory);

                        // Проверяем, пуста ли папка (считаем размер содержимого)
                        long folderSize = CalculateFolderSize(dirInfo);

                        if (folderSize == 0)
                        {
                            Console.WriteLine($"✓ Пустая папка (0 байт): {directory}");
                            emptyFolders.Add(directory);
                        }
                        else
                        {
                            Console.WriteLine($"- Папка с данными ({FormatSize(folderSize)}): {directory}");
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"✗ Нет доступа к папке: {directory}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Ошибка при проверке папки {directory}: {ex.Message}");
                    }
                }

                if (emptyFolders.Count == 0)
                {
                    Console.WriteLine("\nПустых папок не найдено.");
                    return;
                }

                Console.WriteLine($"\n=============================================");
                Console.WriteLine($"Найдено пустых папок: {emptyFolders.Count}");
                Console.WriteLine($"=============================================\n");

                // Запрашиваем подтверждение
                Console.Write($"Переместить эти {emptyFolders.Count} папок в корзину? (да/нет): ");
                string response = Console.ReadLine()?.ToLower();

                if (response == "да" || response == "yes" || response == "y" || response == "д")
                {
                    Console.WriteLine("\nПеремещение папок в корзину...");
                    int movedCount = 0;

                    foreach (var folder in emptyFolders)
                    {
                        try
                        {
                            // Перемещаем папку в корзину
                            FileSystem.DeleteDirectory(folder,
                                UIOption.OnlyErrorDialogs,
                                RecycleOption.SendToRecycleBin);

                            Console.WriteLine($"✓ Перемещено: {folder}");
                            movedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"✗ Ошибка при удалении {folder}: {ex.Message}");
                        }
                    }

                    Console.WriteLine($"\nГотово! Перемещено папок: {movedCount} из {emptyFolders.Count}");
                }
                else
                {
                    Console.WriteLine("Операция отменена.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        /// <summary>
        /// Вычисляет общий размер папки (включая все файлы и подпапки)
        /// </summary>
        static long CalculateFolderSize(DirectoryInfo directory)
        {
            long size = 0;

            try
            {
                // Суммируем размер всех файлов в папке
                FileInfo[] files = directory.GetFiles();
                foreach (FileInfo file in files)
                {
                    try
                    {
                        size += file.Length;
                    }
                    catch
                    {
                        // Пропускаем файлы, к которым нет доступа
                    }
                }

                // Рекурсивно суммируем размер всех подпапок
                DirectoryInfo[] subDirectories = directory.GetDirectories();
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    try
                    {
                        size += CalculateFolderSize(subDirectory);
                    }
                    catch
                    {
                        // Пропускаем папки, к которым нет доступа
                    }
                }
            }
            catch
            {
                // Если нет доступа к папке, считаем что она не пустая
                return long.MaxValue;
            }

            return size;
        }

        /// <summary>
        /// Форматирует размер в байтах в читаемый вид
        /// </summary>
        static string FormatSize(long bytes)
        {
            string[] sizes = { "байт", "КБ", "МБ", "ГБ", "ТБ" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}