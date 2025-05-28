namespace KolokwiumPoprawkowe.Models.DTOs;

public class ClientDTO
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public List<RentalDTO> Rentals { get; set; }
}

public class RentalDTO
{
    public string Vin { get; set; }
    public string Color { get; set; }
    public string Model { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int TotalPrice { get; set; }
}