namespace TravelAgencyAPI.Exceptions
{
    public class TripNotFoundException : Exception
    {
        public TripNotFoundException(int tripId) 
            : base($"Wycieczka o ID {tripId} nie odnaleziona")
        {
        }
    }
}