namespace TravelAgencyAPI.Exceptions
{
    public class MaxParticipantsException : Exception
    {
        public MaxParticipantsException(int tripId) 
            : base($"Wycieczka o ID {tripId} nie ma więcej wolnych miejsc")
        {
        }
    }
}