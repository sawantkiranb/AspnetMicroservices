using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;

namespace Discount.API.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0)
        {
            int retryAvailability = retry.Value;

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var configuration = services.GetRequiredService<IConfiguration>();
                var logger = services.GetRequiredService<ILogger<TContext>>();

                try
                {
                    logger.LogInformation("Migrating Postgre SQL database started..");

                    using var connection = new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                    connection.Open();

                    using var command = new NpgsqlCommand
                    {
                        Connection = connection
                    };

                    command.CommandText = "DROP TABLE IF EXISTS Coupon";
                    command.ExecuteNonQuery();

                    command.CommandText = @"CREATE TABLE coupon(Id SERIAL PRIMARY KEY,
                                                                ProductName VARCHAR(24),
                                                                Description TEXT,
                                                                Amount INT)";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO coupon(productname, description, amount)VALUES ('IPhone X', 'IPhone X Discount', 150);";
                    command.ExecuteNonQuery();

                    command.CommandText = "INSERT INTO coupon(productname, description, amount)VALUES ('Huawei Plus', 'Huawei Discount', 200);";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Migrated Postgre SQL database..");
                }
                catch (Exception)
                {
                    logger.LogError("Error occured while migrating Postgre SQL database");

                    if (retryAvailability < 50)
                    {
                        retryAvailability++;
                        System.Threading.Thread.Sleep(2000);
                        MigrateDatabase<TContext>(host, retryAvailability);
                    }
                }
            }

            return host;
        }
    }
}
