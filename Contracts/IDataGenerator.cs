using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IDataGenerator : IService
    {
        Task GenerateData(int numberOfUsers, int numberOfPosts);
    }
}
