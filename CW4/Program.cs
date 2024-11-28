using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace StudentMarksApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string providerName = configuration["ProviderName"];
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            while (true)
            {
                Console.WriteLine("\n--- Меню ---");
                Console.WriteLine("1. Обрати СКБД");
                Console.WriteLine("2. Підключитися до бази даних");
                Console.WriteLine("3. Відобразити всю інформацію");
                Console.WriteLine("4. Відобразити ПІБ усіх студентів");
                Console.WriteLine("5. Відобразити всі середні оцінки");
                Console.WriteLine("6. Показати студентів із мінімальною оцінкою > N");
                Console.WriteLine("7. Заміряти час виконання запиту");
                Console.WriteLine("8. Оновити інформацію студента");
                Console.WriteLine("9. Видалити інформацію студента");
                Console.WriteLine("0. Вийти");

                Console.Write("Ваш вибір: ");
                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine("Доступні провайдери:");
                            Console.WriteLine("1. SQL Server");
                            Console.WriteLine("2. SQLite");
                            Console.Write("Ваш вибір: ");
                            string dbChoice = Console.ReadLine();

                            if (dbChoice == "1")
                            {
                                providerName = "System.Data.SqlClient";
                                connectionString = configuration.GetConnectionString("DefaultConnection");
                            }
                            else if (dbChoice == "2")
                            {
                                providerName = "System.Data.SQLite";
                                connectionString = "Data Source=StudentDatabase.db;";
                            }
                            else
                            {
                                Console.WriteLine("Невірний вибір. Використовується SQL Server.");
                            }
                            break;

                        case "2":
                            await ConnectToDatabaseAsync(providerName, connectionString);
                            break;

                        case "3":
                            await ExecuteQueryAsync(providerName, connectionString, "SELECT * FROM StudentMarks");
                            break;

                        case "4":
                            await ExecuteQueryAsync(providerName, connectionString, "SELECT StudentName FROM StudentMarks");
                            break;

                        case "5":
                            await ExecuteQueryAsync(providerName, connectionString, "SELECT AVG(Mark) AS AverageMark FROM StudentMarks");
                            break;

                        case "6":
                            Console.Write("Введіть мінімальну оцінку: ");
                            decimal minMark = decimal.Parse(Console.ReadLine());
                            await ExecuteQueryAsync(providerName, connectionString, $"SELECT StudentName FROM StudentMarks WHERE Mark > {minMark}");
                            break;

                        case "7":
                            await MeasureQueryTimeAsync(providerName, connectionString, "SELECT * FROM StudentMarks");
                            break;

                        case "8":
                            Console.Write("Введіть ID студента для оновлення: ");
                            int updateId = int.Parse(Console.ReadLine());
                            Console.Write("Введіть нову оцінку: ");
                            decimal newMark = decimal.Parse(Console.ReadLine());
                            await ExecuteNonQueryAsync(providerName, connectionString, $"UPDATE StudentMarks SET Mark = {newMark} WHERE Id = {updateId}");
                            break;

                        case "9":
                            Console.Write("Введіть ID студента для видалення: ");
                            int deleteId = int.Parse(Console.ReadLine());
                            await ExecuteNonQueryAsync(providerName, connectionString, $"DELETE FROM StudentMarks WHERE Id = {deleteId}");
                            break;

                        case "0":
                            Console.WriteLine("До побачення!");
                            return;

                        default:
                            Console.WriteLine("Невірний вибір, спробуйте ще раз.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
            }
        }

        static async Task ConnectToDatabaseAsync(string providerName, string connectionString)
        {
            var factory = DbProviderFactories.GetFactory(providerName);

            using (DbConnection connection = factory.CreateConnection())
            {
                if (connection == null)
                {
                    Console.WriteLine("Не вдалося створити з'єднання.");
                    return;
                }

                connection.ConnectionString = connectionString;

                try
                {
                    await connection.OpenAsync();
                    Console.WriteLine("Підключення до бази даних успішне!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка підключення: {ex.Message}");
                }
            }
        }

        static async Task ExecuteQueryAsync(string providerName, string connectionString, string query)
        {
            var factory = DbProviderFactories.GetFactory(providerName);

            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                await connection.OpenAsync();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (DbDataReader reader = await command.ExecuteReaderAsync())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write($"{reader.GetName(i)}\t");
                        }
                        Console.WriteLine();

                        while (await reader.ReadAsync())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.Write($"{reader.GetValue(i)}\t");
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }
        }

        static async Task ExecuteNonQueryAsync(string providerName, string connectionString, string query)
        {
            var factory = DbProviderFactories.GetFactory(providerName);

            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                await connection.OpenAsync();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    int affectedRows = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Запит виконано. Змінено рядків: {affectedRows}");
                }
            }
        }

        static async Task MeasureQueryTimeAsync(string providerName, string connectionString, string query)
        {
            var factory = DbProviderFactories.GetFactory(providerName);

            using (DbConnection connection = factory.CreateConnection())
            {
                connection.ConnectionString = connectionString;

                await connection.OpenAsync();

                Stopwatch stopwatch = Stopwatch.StartNew();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    await command.ExecuteNonQueryAsync();
                }

                stopwatch.Stop();
                Console.WriteLine($"Час виконання запиту: {stopwatch.ElapsedMilliseconds} мс");
            }
        }
    }
}
