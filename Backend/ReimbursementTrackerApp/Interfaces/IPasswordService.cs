namespace ReimbursementTrackerApp.Interfaces
{
    public interface IPasswordService
    {
            public byte[] HashPassword(string password, byte[]? dbHashKey, out byte[]? hashkey);
        
    }

}

