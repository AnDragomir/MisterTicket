using MisterTicketApi.Entities;

namespace MisterTicketApi.Services.ServicesInterfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}