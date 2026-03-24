using ReimbursementTrackerApp.Models.DTOs;

namespace ReimbursementTrackerApp.Interfaces
{
    public interface ITokenService
    {
        public string CreateToken(TokenPayloadDto payloadDto);
    }
}
