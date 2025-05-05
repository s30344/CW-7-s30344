namespace TravelAgencyAPI.Models.DTOs;

public class ClientTripDTO
{
    public TripDTO Trip { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? PaymentDate { get; set; }
}