using Microsoft.AspNetCore.Mvc;
using TravelAgencyAPI.Models.DTOs;
using TravelAgencyAPI.Services;
using TravelAgencyAPI.Exceptions;

namespace TravelAgencyAPI.Controllers;



[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    private readonly TripService _tripService;

    public TripsController(TripService tripService)
    {
        _tripService = tripService;
    }

    
    // pobiera listę wszystkich wycieczek wraz z informacjami o krajach

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripDTO>>> GetTrips()
    {
        
        //dla każdej wycieczki dodatkowe zapytanie o kraje
        
        try
        {
            var trips = await _tripService.GetAllTripsAsync();
            return Ok(trips);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd: {ex.Message}");
        }
    }
    
    //pobiera wycieczki powiązane z konkretnym klientem

    [HttpGet("clients/{idClient}/trips")]
    public async Task<ActionResult<IEnumerable<ClientTripDTO>>> GetClientTrips(int idClient)
    {
        
        //sprawdzenie czy klient istnieje
        // pobranie wycieczek klienta
        // dla każdej wycieczki zapytanie o kraje
       
        try
        {
            var clientTrips = await _tripService.GetClientTripsAsync(idClient);
            return Ok(clientTrips);
        }
        catch (ClientNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd: {ex.Message}");
        }
    }


    // rejestruje klienta na wycieczkę

    [HttpPut("clients/{idClient}/trips/{idTrip}")]
    public async Task<IActionResult> AssignClientToTrip(int idClient, int idTrip)
    {
        
        //sprawdzenie czy klient istnieje
        //sprawdzenie czy wycieczka istnieje
        //sprawdzenie czy klient jest już zarejestrowany
        //sprawdzenie dostępności miejsc
        //rejestracja klienta
        
        try
        {
            await _tripService.AssignClientToTripAsync(idClient, idTrip);
            return Ok("Klient przypisany do wycieczki");
        }
        catch (ClientNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (TripNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (MaxParticipantsException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd: {ex.Message}");
        }
    }

    
    //usuwa rejestrację klienta z wycieczki

    [HttpDelete("clients/{idClient}/trips/{idTrip}")]
    public async Task<IActionResult> RemoveClientFromTrip(int idClient, int idTrip)
    {
        
        // sprawdzenie czy rejestracja istnieje
        // usunięcie rejestracji
        
        try
        {
            await _tripService.RemoveClientFromTripAsync(idClient, idTrip);
            return Ok("Klient usunięty z wycieczki");
        }
        catch (RegistrationNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd: {ex.Message}");
        }
    }
}