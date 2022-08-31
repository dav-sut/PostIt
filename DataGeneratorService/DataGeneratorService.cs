using Contracts;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;

namespace DataGeneratorService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class DataGeneratorService : StatelessService, IDataGenerator
    {
        private ContentGenerator _contentGenerator = new ContentGenerator();

        public DataGeneratorService(StatelessServiceContext context)
            : base(context)
        { }

        public async Task GenerateData(int numberOfUsers, int numberOfPosts)
        {
            List<Task> tasks = new List<Task>(numberOfUsers);

            for (int i = 0; i < numberOfUsers; i++)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"DataGenerator - Creating user {i}.");

                Task task = GenerateUserAndPosts(i, numberOfPosts);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task GenerateUserAndPosts(int userId, int numberOfPosts)
        {
            await CreateUser(userId);
            ServiceEventSource.Current.ServiceMessage(Context, $"DataGenerator - User {userId} created.");

            await GeneratePosts(userId, numberOfPosts);
            ServiceEventSource.Current.ServiceMessage(Context, $"DataGenerator - User {userId} created {numberOfPosts} posts.");
        }

        private async Task CreateUser(int userId)
        {
            Uri _authenticatorServiceUri = new Uri("fabric:/Postit/AuthenticatorService");
            int _partitionCount = 5;

            long partitionId = (long)userId % _partitionCount;
            IAuthenticator authenticator = ServiceProxy.Create<IAuthenticator>(_authenticatorServiceUri, new ServicePartitionKey(partitionId));

            await authenticator.RegisterNewUser(userId.ToString(), userId.ToString());
        }

        private async Task GeneratePosts(int userId, int numberOfPosts)
        {
            List<Task> tasks = new List<Task>(numberOfPosts);

            for (int i = 0; i < numberOfPosts; i++)
            {
                Task task = CreatePost(userId);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private async Task CreatePost(int userId)
        {
            PostData postData = new PostData();
            postData.UserId = userId.ToString();
            postData.Timestamp = DateTime.Now;
            postData.Content = _contentGenerator.MakeContent();// "dummy";

            await InsertPost(postData);
        }

        private async Task InsertPost(PostData postData)
        {
            Uri serviceUri = new Uri("fabric:/Postit/PostManagementService");

            IPostOperator postOperator = ServiceProxy.Create<IPostOperator>(serviceUri, new ServicePartitionKey(0));

            await postOperator.InsertPost(postData);
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
