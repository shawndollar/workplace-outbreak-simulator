using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorWebApp.Services;

namespace WorkplaceOutbreakSimulatorWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private SimulatorEngine GetSimulatorEngineInstance()
        {
            SimulatorConfiguration sc = SimulatorConfigManager.GetDefaultConfiguration();
            return new SimulatorEngine(sc);
        }

        private EmployeeDataSource GetEmployeeDataSourceInstance()
        {
            const string section = "AppSettings:EmployeeDataSource";
            string apiUri = Configuration[$"{section}:ApiUri"];
            string apiKey = Configuration[$"{section}:ApiKey"];
            return new EmployeeDataSource(apiUri, apiKey);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddTransient<IEmployeeDataSource>(f => GetEmployeeDataSourceInstance());
            services.AddTransient<IWebAppService, WebAppService>();
            services.AddSingleton<SimulatorEngine>(GetSimulatorEngineInstance());            
        }
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
