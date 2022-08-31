using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Fabric;
using System.Fabric.Query;

namespace ClientWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : Controller//Base
    {
        private Uri _serviceUri;
        private long _partitionCount = -1;

        public AuthenticationController()
        {
            _serviceUri = new Uri("fabric:/Postit/AuthenticatorService");
            _partitionCount = GetPartitionCount().Result;
        }

        [HttpPost]
        [Route("RegisterUser")]
        public async Task<bool> RegisterUser([FromQuery] string userName, [FromQuery] string password)
        {
            IAuthenticator authenticator = GetServiceProxy(userName);
            return await authenticator.RegisterNewUser(userName, password);
        }       

        [HttpPost]
        [Route("LoginUser")]
        public async Task<string> LoginUser([FromQuery] string userName, [FromQuery] string password)
        {
            IAuthenticator authenticator = GetServiceProxy(userName);
            return await authenticator.LoginUser(userName, password);
        }

        [HttpPost]
        [Route("LogoutUser")]
        public async Task<bool> LogoutUser([FromQuery] string userName)
        {
            IAuthenticator authenticator = GetServiceProxy(userName);
            return await authenticator.LogoutUser(userName);
        }

        [HttpGet]
        [Route("IsSessionValid")]
        public async Task<bool> IsSessionValid([FromQuery] string userName, [FromQuery] string sessionId)
        {
            IAuthenticator authenticator = GetServiceProxy(userName);
            return await authenticator.IsSessionValid(userName, sessionId);
        }

        private IAuthenticator GetServiceProxy(string userName)
        {
            long partitionId = (long)userName[0] % _partitionCount;
            return ServiceProxy.Create<IAuthenticator>(_serviceUri, new ServicePartitionKey(partitionId));
        }

        private async Task<long> GetPartitionCount()
        {
            ServicePartitionList partitions;

            using (FabricClient? client = new FabricClient())
            {
                partitions = await client.QueryManager.GetPartitionListAsync(_serviceUri);
            }

            return partitions.Count;
        }
    }
}
