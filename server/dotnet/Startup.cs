using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stripe;

namespace server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            StripeConfiguration.AppInfo = new AppInfo
            {
                Name = "stripe-samples/checkout-single-subscription",
                Url = "https://github.com/stripe-samples/checkout-single-subscription",
                Version = "0.0.1",
            };

            services.Configure<StripeOptions>(options =>
            {
                options.PublishableKey = Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY");
                options.SecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
                options.WebhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");
                options.BasicPrice = Environment.GetEnvironmentVariable("BASIC_PRICE_ID");
                options.ProPrice = Environment.GetEnvironmentVariable("PRO_PRICE_ID");
                options.Domain = Environment.GetEnvironmentVariable("DOMAIN");
            });




            services.AddControllersWithViews(); // or...

            // services.AddControllersWithViews().AddNewtonsoftJson(options =>
            // {
            //     options.SerializerSettings.ContractResolver = new DefaultContractResolver
            //     {
            //         NamingStrategy = new SnakeCaseNamingStrategy(),
            //     };
            // });
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
            //app.UseHttpsRedirection();
            app.UseFileServer();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                // endpoints.MapControllerRoute(
                //     name: "default",
                //     pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
