using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderAppWeb.API.Context;
using OrderAppWeb.API.Profile;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.MSSqlServer;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

#region MyServices

string connectionString = builder.Configuration.GetConnectionString("SQLServer");
builder.Services.AddDbContext<OrderDbContext>(opt =>
{
    opt.UseSqlServer(connectionString, builder => builder.MigrationsAssembly(typeof(OrderDbContext).Assembly.FullName));
});

builder.Services.AddMemoryCache();
builder.Services.AddAutoMapper(typeof(MapperProfile).Assembly); //Memory Cache

//Loglama
Logger log = new LoggerConfiguration()
                //.WriteTo.File("logs/log.txt")
                .WriteTo.MSSqlServer(connectionString, sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs" })
                .MinimumLevel.Information()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

builder.Host.UseSerilog(log);

#endregion



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
