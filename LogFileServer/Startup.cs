using System.IO;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace LogFileServer
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration) => Configuration = configuration;
        public void ConfigureServices(IServiceCollection services) => services.AddControllers();
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".log"] = "application/text";

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Resources")),
                ContentTypeProvider = provider,
                RequestPath = "/resources"
            });

            app.Run(async context => 
            {
                var content = "<h1>Hello Parallel World! Hello Enterprise Developers! ;)</h1>";
                await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(content)); //!SPOT: Pipelines
            });
        }
    }
}
