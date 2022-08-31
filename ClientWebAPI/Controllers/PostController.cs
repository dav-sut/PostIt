using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace ClientWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostController : ControllerBase
    {
        private Uri _serviceUri;

        public PostController()
        {
            _serviceUri = new Uri("fabric:/Postit/PostManagementService");
        }

        [HttpGet]
        [Route("RetrievePost")]       
        public async Task<PostData> GetPost([FromQuery] long postId)
        {
            IPostOperator postOperator = GetServiceProxy();
            PostData postData = await postOperator.RetrievePost(postId);
            return postData;
        }

        [HttpPost]
        [Route("MakePost")]
        public async Task MakeNewPost([FromQuery] PostData postData)
        {
            IPostOperator postOperator = GetServiceProxy();
            await postOperator.InsertPost(postData);
        }

        [HttpGet]
        [Route("GetPostsByUser")]
        public async Task<IEnumerable<PostData>> GetPostsByUser([FromQuery] string userId)
        {
            IPostOperator postOperator = GetServiceProxy();
            IEnumerable<PostData> postData = await postOperator.RetrieveUserPosts(userId);
            return postData;
        }

        private IPostOperator GetServiceProxy()
        {
            return ServiceProxy.Create<IPostOperator>(_serviceUri, new ServicePartitionKey(0));
        }
    }
}