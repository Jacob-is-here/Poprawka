using KolokwiumPoprawkowe.Models.DTOs;

namespace KolokwiumPoprawkowe.Services;

public interface IDbService
{
    Task<ClientDTO> GetClientAsync(int id);
    Task<int> DoesClientExist(int id);
    Task AddNewClientRent(NewRentClientDTO newRentClientDto);
}