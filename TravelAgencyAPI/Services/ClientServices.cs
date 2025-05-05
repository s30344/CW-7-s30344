using System.Data;
using Microsoft.Data.SqlClient;
using TravelAgencyAPI.Models;
using TravelAgencyAPI.Models.DTOs;
using TravelAgencyAPI.Exceptions;

namespace TravelAgencyAPI.Services;

public class ClientService
{
    private readonly string _connectionString;

    public ClientService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> CreateClientAsync(ClientDTO clientDto)
    {
        if (string.IsNullOrWhiteSpace(clientDto.FirstName))
            throw new ArgumentException("Musisz podać imię");
        
        if (string.IsNullOrWhiteSpace(clientDto.LastName))
            throw new ArgumentException("Musisz podać nazwisko");
        
        if (string.IsNullOrWhiteSpace(clientDto.Email))
            throw new ArgumentException("Musisz podać e-mail");
        
        if (!clientDto.Email.Contains('@'))
            throw new ArgumentException("Błędny e-mail");
        
        if (clientDto.Pesel != null && clientDto.Pesel.Length != 11)
            throw new ArgumentException("Pesel musi mieć 11 znaków");

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            var query = @"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
            
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FirstName", clientDto.FirstName);
                command.Parameters.AddWithValue("@LastName", clientDto.LastName);
                command.Parameters.AddWithValue("@Email", clientDto.Email);
                
                command.Parameters.AddWithValue("@Telephone", 
                    string.IsNullOrWhiteSpace(clientDto.Telephone) ? DBNull.Value : clientDto.Telephone);
                
                command.Parameters.AddWithValue("@Pesel", 
                    string.IsNullOrWhiteSpace(clientDto.Pesel) ? DBNull.Value : clientDto.Pesel);
                
                var newId = (int)await command.ExecuteScalarAsync();
                return newId;
            }
        }
    }
}