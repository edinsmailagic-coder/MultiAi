using FluentMigrator.Runner;
using System.Reflection;

namespace MultiAI.Config
{
    public static class FluentMigrationConfig
    {
        public static IServiceCollection AddDatabaseMigrations(this IServiceCollection services,IConfiguration configuration)
        {
            var conString = configuration.GetConnectionString("DefaultConnection");

            services.AddFluentMigratorCore().ConfigureRunner(x => x.AddSqlServer().WithGlobalConnectionString(conString)
            .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations())
                .AddLogging(log => log.AddFluentMigratorConsole());

            return services;
        }
        public static void RunMigrations(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            runner.MigrateUp();
        }
    }
}
