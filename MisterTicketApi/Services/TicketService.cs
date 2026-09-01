using Microsoft.EntityFrameworkCore;
using MisterTicketApi.Database;
using MisterTicketApi.Entities;
using MisterTicketApi.Services.ServicesInterfaces;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MisterTicketApi.Services;

public class TicketService : ITicketService
{
    private readonly MisterTicketContext _context;

    public TicketService(MisterTicketContext context)
    {
        _context = context;
    }

    public async Task<byte[]?> BuildPdfAsync(int reservationId, int userId)
    {
        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Event).ThenInclude(e => e.Venue)
            .Include(r => r.User)
            .Include(r => r.Payment)
            .Include(r => r.EventSeats).ThenInclude(es => es.Seat).ThenInclude(s => s.PricingZone)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.UserId == userId);

        if (reservation is null)
            return null;

        if (reservation.Status != ReservationStatus.Paid)
            throw new InvalidOperationException("Only a paid reservation has a ticket.");

        var reference = reservation.Payment?.Reference ?? $"RES-{reservation.Id}";
        var qr = BuildQrPng($"MISTERTICKET|{reference}|RES{reservation.Id}");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(text => text.FontSize(11).FontColor("#1B1218"));

                page.Header().Column(header =>
                {
                    header.Item().Text("MisterTicket")
                        .FontSize(22).SemiBold().FontColor("#C9A227");
                    header.Item().PaddingTop(2).Text("Electronic ticket")
                        .FontSize(9).FontColor("#6B5B63");
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(content =>
                {
                    content.Spacing(18);

                    content.Item().Text(reservation.Event.Name).FontSize(20).SemiBold();

                    content.Item().Text(
                        $"{reservation.Event.Venue.Name}" +
                        (reservation.Event.Venue.City is null ? "" : $", {reservation.Event.Venue.City}"));

                    content.Item().Text(
                        reservation.Event.StartsAt.ToString("dddd d MMMM yyyy 'at' HH:mm"));

                    content.Item().LineHorizontal(1).LineColor("#DDD5CC");

                    // Seats
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                        });

                        table.Header(head =>
                        {
                            head.Cell().Text("Seat").SemiBold().FontSize(9);
                            head.Cell().Text("Zone").SemiBold().FontSize(9);
                            head.Cell().AlignRight().Text("Price").SemiBold().FontSize(9);
                        });

                        foreach (var seat in reservation.EventSeats
                                     .OrderBy(es => es.Seat.RowLabel)
                                     .ThenBy(es => es.Seat.Number))
                        {
                            table.Cell().PaddingVertical(4)
                                .Text($"{seat.Seat.RowLabel}{seat.Seat.Number}");
                            table.Cell().PaddingVertical(4)
                                .Text(seat.Seat.PricingZone.Name);
                            table.Cell().PaddingVertical(4).AlignRight()
                                .Text($"€ {seat.Price:0.00}");
                        }
                    });

                    content.Item().LineHorizontal(1).LineColor("#DDD5CC");

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(details =>
                        {
                            details.Spacing(4);
                            details.Item().Text($"Total paid: € {reservation.TotalAmount:0.00}").SemiBold();
                            details.Item().Text($"Reference: {reference}").FontSize(9);
                            details.Item().Text(
                                    $"Holder: {reservation.User.FirstName} {reservation.User.LastName}")
                                .FontSize(9);
                            details.Item().Text($"Paid on: {reservation.Payment?.PaidAt:d MMMM yyyy}")
                                .FontSize(9).FontColor("#6B5B63");
                        });

                        row.ConstantItem(120).Column(code =>
                        {
                            code.Item().Width(120).Height(120).Image(qr);
                            code.Item().PaddingTop(4).AlignCenter()
                                .Text("Scan at the door").FontSize(7).FontColor("#6B5B63");
                        });
                    });
                });

                page.Footer().AlignCenter()
                    .Text("MisterTicket · school project · this ticket is not valid for a real performance")
                    .FontSize(7).FontColor("#9A8C93");
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>A real QR code, encoding a string nothing actually checks.</summary>
    private static byte[] BuildQrPng(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(10);
    }
}