namespace TravelAgencyAPI.Exceptions
{
    public class ClientNotFoundException : Exception
    {
        public ClientNotFoundException(int clientId) 
            : base($"Klient o ID {clientId} nie odnaleziony")
        {
        }
    }
}