using Confluent.Kafka;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Popug.Auth.Data;
using Popug.Auth.Infrastructure.Kafka;
using Popug.Auth.Infrastructure.Security;

namespace Popug.Auth;

public class Program
{
    const string AllowDevOrigins = "_allowDevOrigins";
    
    public static void Main(string[] args)
    {
        if (args.Contains("--migrate"))
        {
            // Need to use ConfigurationBuilder to retrieve connection string from appsettings.json
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
            optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));


            try
            {
                // There is no DI at current step, so need to create context manually
                using var dbContext = new AuthDbContext(optionsBuilder.Options);
                dbContext.Database.MigrateAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            Console.WriteLine("Migration success");
            return;
        }
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(name: AllowDevOrigins,
                policy  =>
                {
                    policy.WithOrigins(
                        "http://localhost:8080",
                        "http://localhost:8081")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });
        
        builder.Services.AddSingleton<ClientHandle>();
        builder.Services.AddSingleton<Producer<Null, string>>();
        
        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<AuthDbContext>(options =>
            options.UseSqlite(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        
        builder.Services.AddTransient<Cryptor>();

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth", Version = "v1" });
        });

        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(AllowDevOrigins);
            app.MapOpenApi();
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllers();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("v1/swagger.json", "Auth v1");
        });
        app.Run();
    }
}