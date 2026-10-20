using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Skyzer.Shared
{
    /// <summary>
    /// Shared MongoDB registration so Sync, Sniper, and Dashboard wire the client and
    /// database the same way instead of repeating it in each entry point.
    /// </summary>
    public static class MongoServiceCollectionExtensions
    {
        /// <summary>
        /// The name of the Skyblock database.
        /// </summary>
        public const string DatabaseName = "skyblock";

        /// <summary>
        /// Registers a singleton <see cref="IMongoClient"/> and the Skyblock
        /// <see cref="IMongoDatabase"/> using the "MongoDB" connection string.
        /// </summary>
        public static IServiceCollection AddSkyzerMongo(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddSingleton<IMongoClient>(
                new MongoClient(configuration.GetConnectionString("MongoDB")));
            services.AddSingleton<IMongoDatabase>(
                sp => sp.GetRequiredService<IMongoClient>().GetDatabase(DatabaseName));

            return services;
        }
    }
}
