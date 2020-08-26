using System;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TestApi.Common.Configuration;
using TestApi.DAL;
using TestApi.Extensions;
using TestApi.Telemetry;
using TestApi.ValidationMiddleware;
using TestApi.Validations;

namespace TestApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSwagger();

            services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(host => true)
                        .AllowCredentials();
                }));

            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:InstrumentationKey"]);
            services.AddApplicationInsightsTelemetryProcessor<SuccessfulDependencyProcessor>();

            services.AddJsonOptions();

            services.Configure<AzureAdConfiguration>(options => Configuration.Bind("AzureAd", options));
            services.Configure<ServicesConfiguration>(options => Configuration.Bind("Services", options));
            services.Configure<UserGroupsConfiguration>(options => Configuration.Bind("UserGroups", options));

            services.AddCustomTypes();

            RegisterAuth(services);

            services.AddDbContextPool<TestApiDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("TestApi")));

            services.AddAuthorization(AddPolicies);
            services.AddMvc(AddMvcPolicies);

            services.AddTransient<IRequestModelValidatorService, RequestModelValidatorService>();
            services.AddTransient<IValidatorFactory, RequestModelValidatorFactory>();

            services.AddMvc(opt => opt.Filters.Add(typeof(RequestModelValidatorFilter)))
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddFluentValidation(fv =>
                    fv.RegisterValidatorsFromAssemblyContaining<AllocateUsersRequestValidator>());
        }

        private void RegisterAuth(IServiceCollection services)
        {
            var securitySettings = Configuration.GetSection("AzureAd").Get<AzureAdConfiguration>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = $"{securitySettings.Authority}{securitySettings.TenantId}";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ClockSkew = TimeSpan.Zero,
                        ValidateLifetime = true,
                        ValidAudience = securitySettings.ValidAudience
                    };
                });

            services.AddAuthorization(AddPolicies);
            services.AddMvc(AddMvcPolicies);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.RunLatestMigrations();

            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                const string url = "/swagger/v1/swagger.json";
                c.SwaggerEndpoint(url, "Test API V1");
            });

            app.UseRouting();

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });

            app.UseMiddleware<ExceptionMiddleware>();
        }

        private static void AddPolicies(AuthorizationOptions options)
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        }

        private static void AddMvcPolicies(MvcOptions options)
        {
            options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser().Build()));
        }
    }
}