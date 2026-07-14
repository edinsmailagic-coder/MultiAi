using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IJwtService
    {
        string CreateToken(Users users);
    }
}
