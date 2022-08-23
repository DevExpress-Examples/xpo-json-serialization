using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XpoSerialization.DxSampleModel;

namespace XpoSerialization
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers().AddJsonOptions(options => {
                XPDictionary dictionary = new ReflectionDictionary();
                dictionary.GetDataStoreSchema(typeof(Customer), typeof(Order));
                options.JsonSerializerOptions.Converters.Add(new ChangesSetJsonConverterFactory(null));
                options.JsonSerializerOptions.Converters.Add(new XpoModelJsonConverterFactory(dictionary));
            });
            services.AddCors();
            services.AddHttpContextAccessor();
            services.AddXpoDefaultUnitOfWork(true, (DataLayerOptionsBuilder options) =>
                options.UseConnectionString(Configuration.GetConnectionString("MSSqlServer"))
                 //.UseAutoCreationOption(AutoCreateOption.DatabaseAndSchema) // debug only
                .UseEntityTypes(ConnectionHelper.GetPersistentTypes()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if(env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
