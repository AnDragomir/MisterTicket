using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MisterTicketApi.DTOs;
using MisterTicketApi.Services.ServicesInterfaces;

namespace MisterTicketApi.Controllers;

[ApiController]
[Route("api/reservations/{reservationId:int}")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ITicketService _ticketService;

    public PaymentsController(IPaymentService paymentService, ITicketService ticketService)
    {
        _paymentService = paymentService;
        _ticketService = ticketService;
    }

    // POST: api/reservations/12/payment
    [HttpPost("payment")]
    public async Task<ActionResult<ReservationDTO>> Pay(int reservationId, PaymentCreateDTO dto)
    {
        try
        {
            var paid = await _paymentService.PayAsync(reservationId, dto, GetCurrentUserId());
            if (paid is null)
                return NotFound();

            return Ok(paid);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // GET: api/reservations/12/payment
    [HttpGet("payment")]
    public async Task<ActionResult<PaymentDTO>> GetPayment(int reservationId)
    {
        var payment = await _paymentService.GetForReservationAsync(reservationId, GetCurrentUserId());
        if (payment is null)
            return NotFound();

        return Ok(payment);
    }

    // GET: api/reservations/12/ticket  -> the PDF
    [HttpGet("ticket")]
    public async Task<IActionResult> GetTicket(int reservationId)
    {
        try
        {
            var pdf = await _ticketService.BuildPdfAsync(reservationId, GetCurrentUserId());
            if (pdf is null)
                return NotFound();

            return File(pdf, "application/pdf", $"misterticket-{reservationId}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}