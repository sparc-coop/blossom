using Bogus;
using Sparc.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Sparc.Tests
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Sparcify(this IServiceCollection services)
        {
            return services;
        }

        public static Faker<T> AddFakeData<T>(this IServiceCollection services) where T : class
        {
            var faker = new Faker<T>();
            return faker;
        }

        public static Faker<T> AddFakeRepository<T>(this IServiceCollection services, int? count = null) where T : class, IRoot<string>
        {
            var faker = services.AddFakeData<T>();

            if (!count.HasValue) 
                count = new Random().Next(1, 20);
            
            services.AddScoped<IRepository<T>>(x => new BogusRepository<T>(faker.Generate(count.Value)));
            return faker;
        }

        public static Faker<T> AddFakeDatum<T>(this IServiceCollection services) where T : class
        {
            var faker = new Faker<T>();
            services.AddSingleton(x => faker.Generate());
            return faker;
        }
    }
}
