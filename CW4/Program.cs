using System.Data.Common;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CW4
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string providerName = configuration["ProviderName"];
            string connectionString = configuration.GetConnectionString("DefaultConnection");
            DbProviderFactories.RegisterFactory(providerName, SqlClientFactory.Instance);

            DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);

            using (DbConnection connection = factory.CreateConnection())
            {
                if (connection == null)
                {
                    Console.WriteLine("Не вдалося створити з'єднання.");
                    return;
                }

                connection.ConnectionString = connectionString;

                while (true)
                {
                    Console.WriteLine("\n--- Меню ---");
                    Console.WriteLine("1. Підключитися до бази даних");
                    Console.WriteLine("2. Від'єднатися від бази даних");
                    Console.WriteLine("3. Відобразити всю інформацію");
                    Console.WriteLine("4. Відобразити ПІБ усіх студентів");
                    Console.WriteLine("5. Відобразити всі середні оцінки");
                    Console.WriteLine("6. Показати студентів із мінімальною оцінкою > N");
                    Console.WriteLine("0. Вийти");

                    Console.Write("Ваш вибір: ");
                    string choice = Console.ReadLine();

                    try
                    {
                        switch (choice)
                        {
                            case "1":
                                ConnectToDatabase(connection);
                                break;
                            case "2":
                                DisconnectFromDatabase(connection);
                                break;
                            case "3":
                                ExecuteQuery(connection, "SELECT * FROM StudentMarks1");
                                break;
                            case "4":
                                ExecuteQuery(connection, "SELECT StudentName FROM StudentMarks1");
                                break;
                            case "5":
                                ExecuteQuery(connection, "SELECT Mark FROM StudentMarks1");
                                break;
                            case "6":
                                Console.Write("Введіть мінімальну оцінку: ");
                                decimal minMark = decimal.Parse(Console.ReadLine());
                                ExecuteQuery(connection, $"SELECT StudentName FROM StudentMarks1 WHERE Mark > {minMark}");
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
        }

        static void ConnectToDatabase(DbConnection connection)
        {
            try
            {
                connection.Open();
                Console.WriteLine("Підключення до бази даних успішне!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка підключення: {ex.Message}");
            }
        }

        static void DisconnectFromDatabase(DbConnection connection)
        {
            try
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                    Console.WriteLine("Від'єднання від бази даних успішне!");
                }
                else
                {
                    Console.WriteLine("Підключення вже закрито.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка від'єднання: {ex.Message}");
            }
        }

        static void ExecuteQuery(DbConnection connection, string query)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                {
                    Console.WriteLine("Будь ласка, спочатку підключіться до бази даних.");
                    return;
                }

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = query;

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        DataTable table = new DataTable();
                        table.Load(reader);

                        foreach (DataColumn column in table.Columns)
                        {
                            Console.Write($"{column.ColumnName}\t");
                        }
                        Console.WriteLine();

                        foreach (DataRow row in table.Rows)
                        {
                            foreach (var item in row.ItemArray)
                            {
                                Console.Write($"{item}\t");
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка виконання запиту: {ex.Message}");
            }
        }
    }
}
