using Contracts;
using FeedGeneratorActor.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace ClientWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FeedGeneratorActorController : ControllerBase
    {
        [HttpGet]
        [Route("GetPostsByUserActor")]
        public async Task<IEnumerable<PostData>> GetPostsByUserActor([FromQuery] string userId)
        {
            Uri serviceUri = new Uri("fabric:/Postit/FeedGeneratorActorService");
            ActorId actorId = new ActorId(userId);

            IFeedGeneratorActor myActor = ActorProxy.Create<IFeedGeneratorActor>(actorId, serviceUri);

            IEnumerable<PostData> postData = await myActor.GetPostsByUser();
            return postData;
        }
    }
}
