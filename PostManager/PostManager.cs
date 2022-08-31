using Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PostManager
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class PostManager : StatefulService, IPostOperator
    {
        public PostManager(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<long> InsertPost(PostData postData)
        {
            IReliableDictionary<long, PostData> postCollection = await StateManager.GetOrAddAsync<IReliableDictionary<long, PostData>>("postCollection");
            IReliableDictionary<string, long> postIdCounter = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("postIdCounter");
            //IReliableDictionary<long, IEnumerable<PostData>> postiCollection = await StateManager.GetOrAddAsync<IReliableDictionary<long, IEnumerable<PostData>>>("postCollection");

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<long> postId = await postIdCounter.TryGetValueAsync(tx, "IdCounter");
                postData.Id = postId.Value;

                ServiceEventSource.Current.ServiceMessage(Context, $"Inserting new post - Id: {postData.Id} Content: {postData.Content}");

                await postIdCounter.AddOrUpdateAsync(tx, "IdCounter", 0, (key, value) => ++value);
                await postCollection.AddOrUpdateAsync(tx, postData.Id, postData, (key, value) => value);

                await tx.CommitAsync();

                return postData.Id;
            }
        }

        public async Task<PostData> RetrievePost(long postId)
        {
            ServiceEventSource.Current.ServiceMessage(Context, $"Retrieving post with Id: {postId}");
            IReliableDictionary<long, PostData> postCollection = await StateManager.GetOrAddAsync<IReliableDictionary<long, PostData>>("postCollection");

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<PostData> postData = await postCollection.TryGetValueAsync(tx, postId);              
                await tx.CommitAsync();

                if (postData.HasValue)
                {
                    ServiceEventSource.Current.ServiceMessage(Context, $"Post with Id: {postId} retrieved. Content: {postData.Value.Content}");
                    return postData.Value;
                }

                ServiceEventSource.Current.ServiceMessage(Context, $"Post with Id: {postId} could not be found.");
                throw new KeyNotFoundException("");
            }
        }

        public async Task<IEnumerable<PostData>> RetrieveUserPosts(long userId)
        {
            await Task.Delay(10);
            throw new NotImplementedException();
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
          // return new List<ServiceReplicaListener>();
            return this.CreateServiceRemotingReplicaListeners();
            /*return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        return new WebHostBuilder()
                                    .UseKestrel()
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatefulServiceContext>(serviceContext)
                                            .AddSingleton<IReliableStateManager>(this.StateManager))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };*/
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic 
            //       or remove this RunAsync override if it's not needed in your service.

            IReliableDictionary<string, long> myDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("myDictionary");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<long> result = await myDictionary.TryGetValueAsync(tx, "Counter");

                    ServiceEventSource.Current.ServiceMessage(this.Context, "Current Counter Value: {0}", result.HasValue ? result.Value.ToString() : "Value does not exist.");

                    await myDictionary.AddOrUpdateAsync(tx, "Counter", 0, (key, value) => ++value);

                    // If an exception is thrown before calling CommitAsync, the transaction aborts, all changes are 
                    // discarded, and nothing is saved to the secondary replicas.
                    await tx.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
