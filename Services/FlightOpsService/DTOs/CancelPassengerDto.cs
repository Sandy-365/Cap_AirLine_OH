using System.ComponentModel.DataAnnotations;

namespace FlightOpsService.DTOs;

public class CancelPassengerDto
{
    [Required(ErrorMessage = "Cancellation reason is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters")]
    public string CancellationReason { get; set; } = "";
}
