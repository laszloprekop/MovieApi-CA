using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MovieApi.BackgroundServices;
using MovieApi.ExceptionHandling;
using MovieContracts;
using MovieCore.DomainContracts;
using MovieData;
using MovieData.Extensions;
using MovieData.Repositories;
using MovieServices;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MovieContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddAutoMapper(
    cfg => cfg.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"],
    typeof(MovieData.Mapping.MovieProfile).Assembly);

// Add services to the container.

builder.Services.AddControllers()
    .AddApplicationPart(typeof(MoviePresentation.PresentationAssemblyReference).Assembly)
    .AddNewtonsoftJson();

// !! Browsers only hand responses to cross-origin JS if we opt in per origin.
// -> Origins live in configuration so the hosted instance can add its own via env.

var clientOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(clientOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("X-Pagination")));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddHostedService<ReviewTrimmer>();

var app = builder.Build();
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.Services.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
}

app.Services.SeedData();

app.Run();