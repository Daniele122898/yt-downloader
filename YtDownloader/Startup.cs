using System;
using System.IO;
using System.Net;
using System.Reflection;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using YtDownloader.Configurations;
using YtDownloader.Extensions;
using YtDownloader.Helper;
using YtDownloader.Services;

namespace YtDownloader
{
    public class Startup
    {
        private readonly ILogger<Startup> _log;

        public Startup(IConfiguration configuration, ILogger<Startup> log)
        {
            _log = log;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo()
                {
                    Title = "YT-Downloader",
                    Version = "v1",
                    Description = "YT-Downloader Backend Service"
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
            
            this.ConfigureServices(services);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddRouting(op => op.LowercaseUrls = true);
            services.AddCors();

            services.AddAutoMapper(typeof(Startup).Assembly);

            services.AddServices(Configuration);
            services.Configure<DownloadConfig>(Configuration.GetSection("DownloadSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<DownloadConfig> downloadConfig)
        {
            string path = Path.Combine(env.ContentRootPath, downloadConfig.Value.OutputPath);
            if (!Directory.Exists(path))
            {
                _log.LogWarning($"Output Path doesn't exist. Creating it at {path}");
                Directory.CreateDirectory(path);
            }
            PathHelper.SetOutputPath(path);

            app.ApplicationServices.GetRequiredService<CacheService>(); // Warmup service
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                
                // Enable middleware to serve generated swagger as a json endpoint
                app.UseSwagger();
                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
                // specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Yt-Downloader");
                });
            }
            else
            {
                app.UseExceptionHandler(builder =>
                {
                    builder.Run(async context =>
                    {
                        context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseRouting();
            
            // Add support for static files. Ordering is important
            app.UseDefaultFiles(); // search index.html in wwwroot
            app.UseStaticFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToController("Index", "Fallback"); // Use our fallback
            });
        }
    }
}