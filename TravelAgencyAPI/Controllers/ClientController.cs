using Microsoft.AspNetCore.Mvc;
using TravelAgencyAPI.Models.DTOs;
using TravelAgencyAPI.Services;

namespace TravelAgencyAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly ClientService _clientService;

    public ClientsController(ClientService clientService)
    {
        _clientService = clientService;
    }
    //tworzy nowego klienta
    [HttpPost]
    public async Task<ActionResult<int>> CreateClient([FromBody] ClientDTO clientDto)
    {
        try
        {
            var newClientId = await _clientService.CreateClientAsync(clientDto);
            return CreatedAtAction(nameof(CreateClient), new { id = newClientId }, newClientId);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Błąd: {ex.Message}");
        }
    }
}