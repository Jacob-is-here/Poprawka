using KolokwiumPoprawkowe.Exceptions;
using KolokwiumPoprawkowe.Models.DTOs;
using Microsoft.Data.SqlClient;

namespace KolokwiumPoprawkowe.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;

    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<ClientDTO> GetClientAsync(int id)
    {
        var client = new ClientDTO()
        {
            Rentals = new List<RentalDTO>()
        };
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();

            string cmd =
                @"select c.id, c.firstname, c.lastname, c.address , c2.VIN, c3.Name ,m.Name, DateFrom , DateTo,TotalPrice
                            from clients c 
                            join car_rentals cr on c.ID = cr.ClientID
                            join cars c2 on c2.ID = cr.CarID
                            join colors c3 on c2.ColorID = c3.ID
                            join models m on m.ID = c2.ModelID
                            where c.ID = @ID";
            using (SqlCommand command = new SqlCommand(cmd,connection))
            {
                command.Parameters.AddWithValue("@ID", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        client.Id = reader.GetInt32(0);
                        client.FirstName = reader.GetString(1);
                        client.LastName = reader.GetString(2);
                        client.Address = reader.GetString(3);

                        client.Rentals.Add(new RentalDTO
                        {
                            Vin = reader.GetString(4),
                            Color = reader.GetString(5),
                            Model = reader.GetString(6),
                            DateFrom = reader.GetDateTime(7),
                            DateTo = reader.GetDateTime(8),
                            TotalPrice = reader.GetInt32(9)
                        });
                    }
                }
            }
        }
        return client;
    }

    public async Task<int> DoesClientExist(int id)
    {
        using (SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await connection.OpenAsync();
            string cmd = "SELECT count(*) FROM clients WHERE id = @id";
            using (SqlCommand command = new SqlCommand(cmd, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                int count = (int)await command.ExecuteScalarAsync();
                return count ;
            }
        }
    }

    public async Task AddNewClientRent(NewRentClientDTO newRentClientDto)
    {
        string czyIstniejeAuto = @"select count(*) from cars where ID = @ID";

        using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("Default")))
        {
            await conn.OpenAsync();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    using (SqlCommand command = new SqlCommand(czyIstniejeAuto,conn,transaction))
                    {
                        command.Parameters.AddWithValue("@ID", newRentClientDto.CarId);
                        var car = await command.ExecuteScalarAsync();
                        if (car == null)
                        {
                            throw new NotFoundException($"Nie istnieje auto o podanym id : {newRentClientDto.CarId}");
                        }
                    }

                    string newClient = @"insert into clients (FirstName, LastName, Address) values (@FirstName, @LastName, @Address)";

                    string getCena = @"select PricePerDay from cars 
                                        join car_rentals cr on cars.ID = cr.CarID
                                        where cr.ID = @ID";

                    int cena;
                    using (SqlCommand command = new SqlCommand(getCena, conn, transaction))
                    {
                        command.Parameters.AddWithValue("@ID", newRentClientDto.CarId);
                        var result = await command.ExecuteScalarAsync();
                        cena = (int)result;
                    }
                    using (SqlCommand command = new SqlCommand(newClient,conn,transaction))
                    {
                        command.Parameters.AddWithValue("@FirstName",newRentClientDto.Client.FirstName);
                        command.Parameters.AddWithValue("@LastName",newRentClientDto.Client.LastName);
                        command.Parameters.AddWithValue("@Address",newRentClientDto.Client.Address);
                        await command.ExecuteNonQueryAsync();
                    }

                    string getClientID = @"select id from clients where FirstName = @FirstName and LastName =@LastName and Address = @Address";
                    int clientID;
                    
                    using (SqlCommand command = new SqlCommand(getClientID,conn,transaction))
                    {
                        command.Parameters.AddWithValue("@FirstName",newRentClientDto.Client.FirstName);
                        command.Parameters.AddWithValue("@LastName",newRentClientDto.Client.LastName);
                        command.Parameters.AddWithValue("@Address",newRentClientDto.Client.Address);
                        var result = await command.ExecuteScalarAsync();
                        clientID = (int)result;
                    }
                    
                    string newRent =
                        @"insert into car_rentals (ClientID, CarID, DateFrom, DateTo, TotalPrice) values (@ClientID, @CarID, @DateFrom, @DateTo, @TotalPrice)";
                    using (SqlCommand command = new SqlCommand(newRent, conn, transaction))
                    {
                        command.Parameters.AddWithValue("@ClientID", clientID);
                        command.Parameters.AddWithValue("@CarID", newRentClientDto.CarId);
                        command.Parameters.AddWithValue("@DateFrom", newRentClientDto.DateFrom);
                        command.Parameters.AddWithValue("@DateTo", newRentClientDto.DateTo);
                        command.Parameters.AddWithValue("@TotalPrice", cena * (newRentClientDto.DateTo - newRentClientDto.DateFrom).Days);
                        await command.ExecuteNonQueryAsync();
                    }
                    transaction.Commit();
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    throw;
                }
                
            }

        }
    }
}