using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MicroNet.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MicroNet.Startup
{
    public class MicroStartup
    {
        public MicroStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.
            AddMvc(o => o.Conventions.Add(
                new GenericControllerRouteConvention()
            )).
            ConfigureApplicationPartManager(m =>
                m.FeatureProviders.Add(new GenericControllerFeatureProvider()
            ));

            services.Scan(scan =>
                scan.FromEntryAssembly().AddClasses(classes =>
                    classes.AssignableTo(typeof(ITransformer<>)))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime()
                );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
