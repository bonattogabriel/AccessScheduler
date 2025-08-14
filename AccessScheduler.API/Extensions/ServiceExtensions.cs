using Microsoft.EntityFrameworkCore;
using AccessScheduler.Api.Data;
using AccessScheduler.Api.Services;
using AccessScheduler.API.Interfaces;

namespace AccessScheduler.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}