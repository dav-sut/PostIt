using Microsoft.ServiceFabric.Services.Remoting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Contracts
{
    public interface IPostOperator : IService
    {
        Task<long> InsertPost(PostData postData);

        Task<PostData> RetrievePost(long postId);

        Task<IEnumerable<PostData>> RetrieveUserPosts(string userId);
    }
}
