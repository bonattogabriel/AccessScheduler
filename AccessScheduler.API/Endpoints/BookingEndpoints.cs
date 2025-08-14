using Microsoft.AspNetCore.Mvc;
using AccessScheduler.Api.Services;
using AccessScheduler.Shared.DTOs;
using System.Globalization;
using AccessScheduler.API.Interfaces;
using AccessScheduler.Shared.Exceptions;
using AccessScheduler.Shared.Extensions;

namespace AccessScheduler.Api.Endpoints;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/book").WithTags("Bookings");

        group.MapPost("/", CreateBooking)
            .WithName("CreateBooking")
            .WithSummary("Cria uma nova reserva")
            .Produces<BookingResponse>(201)
            .Produces<ConflictResponse>(409)
            .Produces<ValidationProblemDetails>(400);

        group.MapDelete("/{id:guid}", CancelBooking)
            .WithName("CancelBooking")
            .WithSummary("Cancela uma reserva")
            .Produces(204)
            .Produces(404);

        app.MapGet("/free-slots", GetFreeSlots)
            .WithTags("Bookings")
            .WithName("GetFreeSlots")
            .WithSummary("Lista horários livres")
            .Produces<List<FreeSlot>>(200)
            .Produces<ValidationProblemDetails>(400);
    }

    private static async Task<IResult> CreateBooking(
        [FromBody] BookingRequest request,
        [FromHeader(Name = "X-Client-TimeZone")] string? clientTimeZone,
        IBookingService bookingService,
        ILogger<Program> logger)
    {
        try
        {
            var timeZone = clientTimeZone ?? "America/Sao_Paulo";

            if (!DateTimeExtensions.IsValidTimeZone(timeZone))
            {
                return Results.BadRequest(new { error = "Timezone inválido" });
            }

            var result = await bookingService.CreateBookingAsync(request, timeZone);

            logger.LogInformation("Booking created: {BookingId} for customer {CustomerName}",
                result.Id, result.CustomerName);

            return Results.Created($"/book/{result.Id}", result);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Validation error: {Error}", ex.Message);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ConcurrencyException ex)
        {
            logger.LogInformation("Booking conflict: {Message}", ex.Message);
            return Results.Conflict(ex.ConflictResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating booking");
            return Results.Problem("Erro interno do servidor");
        }
    }

    private static async Task<IResult> CancelBooking(
        Guid id,
        IBookingService bookingService,
        ILogger<Program> logger)
    {
        try
        {
            var result = await bookingService.CancelBookingAsync(id);

            if (!result)
            {
                return Results.NotFound(new { error = "Reserva não encontrada" });
            }

            logger.LogInformation("Booking cancelled: {BookingId}", id);
            return Results.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling booking {BookingId}", id);
            return Results.Problem("Erro interno do servidor");
        }
    }

    private static async Task<IResult> GetFreeSlots(
        [FromQuery] string date,
        [FromHeader(Name = "X-Client-TimeZone")] string? clientTimeZone,
        IBookingService bookingService,
        ILogger<Program> logger,
        [FromQuery] int duration = 30,
        [FromQuery] string resource = "gate-1")
    {
        try
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
            {
                return Results.BadRequest(new { error = "Formato de data inválido. Use YYYY-MM-DD" });
            }

            if (duration < 15 || duration > 480)
            {
                return Results.BadRequest(new { error = "Duração deve estar entre 15 e 480 minutos" });
            }

            var timeZone = clientTimeZone ?? "America/Sao_Paulo";

            if (!DateTimeExtensions.IsValidTimeZone(timeZone))
            {
                return Results.BadRequest(new { error = "Timezone inválido" });
            }

            var freeSlots = await bookingService.GetFreeSlotsAsync(parsedDate, duration, resource, timeZone);

            return Results.Ok(freeSlots);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting free slots");
            return Results.Problem("Erro interno do servidor");
        }
    }
}