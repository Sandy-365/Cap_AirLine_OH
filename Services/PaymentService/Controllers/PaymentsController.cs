using PaymentService.DTOs;
using PaymentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Retrieves payment transaction details by payment ID.
    /// [Allowed Roles: Passenger, Dealer, Admin, SuperAdmin]
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetPayment(int id)
    {
        try
        {
            var result = await _paymentService.GetPaymentAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Processes or creates a new payment for a booking using Razorpay gateway integration.
    /// [Allowed Roles: Passenger, Dealer]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Passenger,Dealer")]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Initiates a refund for a processed payment transaction.
    /// [Allowed Roles: Admin, SuperAdmin, FinancialAdmin]
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Roles = "Admin,SuperAdmin,FinancialAdmin")]
    public async Task<IActionResult> Refund(int id)
    {
        try
        {
            var result = await _paymentService.RefundAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
