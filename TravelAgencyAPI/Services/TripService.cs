using System.Data;
using Microsoft.Data.SqlClient;
using TravelAgencyAPI.Models;
using TravelAgencyAPI.Models.DTOs;
using TravelAgencyAPI.Exceptions;

namespace TravelAgencyAPI.Services;

public class TripService
{
    private readonly string _connectionString;

    public TripService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<IEnumerable<TripDTO>> GetAllTripsAsync()
    {
        var trips = new List<TripDTO>();
        
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Query for trips
            var tripQuery = @"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople
                FROM Trip t
                ORDER BY t.DateFrom DESC";
            
            using (var command = new SqlCommand(tripQuery, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var trip = new TripDTO
                    {
                        IdTrip = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5)
                    };
                    
                    trips.Add(trip);
                }
            }
            
            // Query for countries for each trip
            foreach (var trip in trips)
            {
                var countryQuery = @"
                    SELECT c.Name
                    FROM Country c
                    JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                    WHERE ct.IdTrip = @IdTrip";
                
                using (var command = new SqlCommand(countryQuery, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", trip.IdTrip);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            trip.Countries.Add(new CountryDTO
                            {
                                Name = reader.GetString(0)
                            });
                        }
                    }
                }
            }
        }
        
        return trips;
    }

    public async Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId)
    {
        var clientTrips = new List<ClientTripDTO>();
        
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // First check if client exists
            var clientExistsQuery = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
            using (var command = new SqlCommand(clientExistsQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                var exists = await command.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new ClientNotFoundException(clientId);
                }
            }
            
            // Query for client's trips
            var query = @"
                SELECT 
                    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                    ct.RegisteredAt, ct.PaymentDate
                FROM Client_Trip ct
                JOIN Trip t ON ct.IdTrip = t.IdTrip
                WHERE ct.IdClient = @IdClient
                ORDER BY ct.RegisteredAt DESC";
            
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                    {
                        return clientTrips; // empty list
                    }
                    
                    while (await reader.ReadAsync())
                    {
                        var trip = new TripDTO
                        {
                            IdTrip = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                            DateFrom = reader.GetDateTime(3),
                            DateTo = reader.GetDateTime(4),
                            MaxPeople = reader.GetInt32(5)
                        };
                        
                        var clientTrip = new ClientTripDTO
                        {
                            Trip = trip,
                            RegisteredAt = reader.GetDateTime(6),
                            PaymentDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                        };
                        
                        clientTrips.Add(clientTrip);
                    }
                }
            }
            
            // Query for countries for each trip
            foreach (var clientTrip in clientTrips)
            {
                var countryQuery = @"
                    SELECT c.Name
                    FROM Country c
                    JOIN Country_Trip ct ON c.IdCountry = ct.IdCountry
                    WHERE ct.IdTrip = @IdTrip";
                
                using (var command = new SqlCommand(countryQuery, connection))
                {
                    command.Parameters.AddWithValue("@IdTrip", clientTrip.Trip.IdTrip);
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            clientTrip.Trip.Countries.Add(new CountryDTO
                            {
                                Name = reader.GetString(0)
                            });
                        }
                    }
                }
            }
        }
        
        return clientTrips;
    }

    public async Task AssignClientToTripAsync(int clientId, int tripId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Check if client exists
            var clientExistsQuery = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
            using (var command = new SqlCommand(clientExistsQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                var exists = await command.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new ClientNotFoundException(clientId);
                }
            }
            
            // Check if trip exists
            var tripExistsQuery = "SELECT 1 FROM Trip WHERE IdTrip = @IdTrip";
            using (var command = new SqlCommand(tripExistsQuery, connection))
            {
                command.Parameters.AddWithValue("@IdTrip", tripId);
                var exists = await command.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new TripNotFoundException(tripId);
                }
            }
            
            // Check if client is already registered for this trip
            var registrationExistsQuery = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            using (var command = new SqlCommand(registrationExistsQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                command.Parameters.AddWithValue("@IdTrip", tripId);
                var exists = await command.ExecuteScalarAsync();
                if (exists != null)
                {
                    throw new Exception("Client is already registered for this trip.");
                }
            }
            
            // Check if trip has available spots
            var maxPeopleQuery = "SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip";
            var currentParticipantsQuery = "SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip";
            
            int maxPeople, currentParticipants;
            
            using (var command = new SqlCommand(maxPeopleQuery, connection))
            {
                command.Parameters.AddWithValue("@IdTrip", tripId);
                maxPeople = (int)await command.ExecuteScalarAsync();
            }
            
            using (var command = new SqlCommand(currentParticipantsQuery, connection))
            {
                command.Parameters.AddWithValue("@IdTrip", tripId);
                currentParticipants = (int)await command.ExecuteScalarAsync();
            }
            
            if (currentParticipants >= maxPeople)
            {
                throw new MaxParticipantsException(tripId);
            }
            
            // Register client for trip
            var insertQuery = @"
                INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
                VALUES (@IdClient, @IdTrip, @RegisteredAt, NULL)";
            
            using (var command = new SqlCommand(insertQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                command.Parameters.AddWithValue("@IdTrip", tripId);
                command.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
                
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task RemoveClientFromTripAsync(int clientId, int tripId)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            
            // Check if registration exists
            var registrationExistsQuery = "SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            using (var command = new SqlCommand(registrationExistsQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                command.Parameters.AddWithValue("@IdTrip", tripId);
                var exists = await command.ExecuteScalarAsync();
                if (exists == null)
                {
                    throw new RegistrationNotFoundException(clientId, tripId);
                }
            }
            
            // Delete registration
            var deleteQuery = "DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            using (var command = new SqlCommand(deleteQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                command.Parameters.AddWithValue("@IdTrip", tripId);
                
                var affectedRows = await command.ExecuteNonQueryAsync();
                if (affectedRows == 0)
                {
                    throw new Exception("Failed to remove client from trip.");
                }
            }
        }
    }
}