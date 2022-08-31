using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IAuthenticator : IService
    {
        Task<bool> IsSessionValid(string userName, string sessionId);
        Task<string> LoginUser(string userName, string password);
        Task<bool> LogoutUser(string userName);
        Task<bool> RegisterNewUser(string userName, string password);
    }
}