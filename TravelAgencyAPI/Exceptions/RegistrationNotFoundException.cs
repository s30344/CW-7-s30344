namespace TravelAgencyAPI.Exceptions
{
    public class RegistrationNotFoundException : Exception
    {
        public RegistrationNotFoundException(int clientId, int tripId) 
            : base($"Rejestracja nie odnaleziona dla klienta o ID {clientId} i wycieczki o ID {tripId}")
        {
        }
    }
}