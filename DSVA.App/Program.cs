using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DSVA.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var node = serviceProvider.GetRequiredService<Node>();

            var r = new Random();
            Console.WriteLine("Hello World!");
            for (int i = 0; i < 100; i++)
            {
                node.Act();
                await Task.Delay(TimeSpan.FromMilliseconds(r.Next(1000)));
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            //logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(dispose: true);
            });

            services.AddTransient<Node>();
        }
    }
}
