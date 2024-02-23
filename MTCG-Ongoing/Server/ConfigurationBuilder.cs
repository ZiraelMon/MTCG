using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace MTCG.Server {
    public static class ConfigurationManager {
        public static IConfigurationRoot Configuration { get; private set; }

        static ConfigurationManager() {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
        }

        public static string GetConnectionString(string name) {
            return Configuration.GetConnectionString(name);
        }
    }
}
