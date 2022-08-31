using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace ClientWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DataGeneratorController : Controller
    {
        [HttpPost]
        [Route("GenerateData")]
        public async Task GenerateData([FromQuery] int numberOfUsers, [FromQuery] int numberOfPosts)
        {
            Uri authenticatorServiceUri = new Uri("fabric:/Postit/DataGeneratorService");
            IDataGenerator authenticator = ServiceProxy.Create<IDataGenerator>(authenticatorServiceUri);
            await authenticator.GenerateData(numberOfUsers, numberOfPosts);
        }
    }
}
