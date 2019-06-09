using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HueBridge.ApplicationMain;
using LoggingMiddleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace HueBridge
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppOptions>(Configuration);
            services.AddSingleton<IGlobalResourceProvider, GlobalResourceProvider>();
            services.AddSingleton<IHostedService, SsdpService>();
            services.AddSingleton<IHostedService, HueBridgeEngine>();

            // Add framework services
            services.AddMvc(options => { options.OutputFormatters.Add(new XmlSerializerOutputFormatter()); })
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver { NamingStrategy = new Utilities.LowercaseNamingStrategy() });
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMiddleware<LogRequestMiddleware>();
            app.UseMiddleware<LogResponseMiddleware>();

            app.UseMvc();
        }
    }
}
