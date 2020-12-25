using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KokazGoodsTransfer.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KokazGoodsTransfer
{
    public class Startup
    {
        //remotlyconnection
        //Data Source = SQL5069.site4now.net; Initial Catalog = DB_A6C91F_Kokaz; User Id = DB_A6C91F_Kokaz_admin; Password=123qwe123
        // Scaffold-DbContext "Server=.;Database=Kokaz;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -F
        //> dotnet ef dbcontext scaffold "Server=.;Database=Kokaz;Trusted_Connection=True;" Microsoft.EntityFrameworkCore.SqlServer -o Models -F
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddTransient(typeof(KokazContext), typeof(KokazContext));
            services.AddCors(options =>
{
    options.AddPolicy("EnableCORS", builder =>
    {
       builder.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
             app.UseCors("EnableCORS");

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
