using user_service.Entities;

namespace user_service.Interfaces;

public interface ITokenProvider
{
    string Create(User user);
}