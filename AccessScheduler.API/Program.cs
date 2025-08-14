using Microsoft.EntityFrameworkCore;
using AccessScheduler.Api.Data;
using AccessScheduler.Api.Extensions;
using AccessScheduler.Api.Endpoints;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(
    builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string not found"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Access Scheduler API", Version = "v1" });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");

app.MapBookingEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .WithName("HealthCheck")
   .WithTags("Health");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (app.Environment.IsDevelopment())
    {
        context.Database.EnsureCreated();

        if (!context.Bookings.Any())
        {
            context.Bookings.AddRange(
                new AccessScheduler.Shared.Models.Booking
                {
                    Id = Guid.NewGuid(),
                    CustomerName = "João Silva",
                    Document = "12345678901",
                    Resource = "gate-1",
                    StartUtc = DateTime.UtcNow.AddHours(2),
                    EndUtc = DateTime.UtcNow.AddHours(2.5),
                    RetratoBase64 = Convert.ToBase64String(new byte[100]),
                    Latitude = -22.9068,
                    Longitude = -43.1729,
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
        }
    }
}

app.Run();