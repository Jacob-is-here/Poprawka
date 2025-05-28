using KolokwiumPoprawkowe.Exceptions;
using KolokwiumPoprawkowe.Models.DTOs;
using KolokwiumPoprawkowe.Services;
using Microsoft.AspNetCore.Mvc;

namespace KolokwiumPoprawkowe.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly IDbService _idbService;

    public ClientsController(IDbService idbService)
    {
        _idbService = idbService;
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> getClient(int id)
    {
        var count = await _idbService.DoesClientExist(id);
        if (count == 0)
        {
            return NotFound($"Nie istnieje klient o podanym id {id}");
        }
        var client = await _idbService.GetClientAsync(id);

        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> newClient([FromBody] NewRentClientDTO newRentClientDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        try
        {
            await _idbService.AddNewClientRent(newRentClientDto);
            return Created();
        }
        catch (NotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        
    }
    
}