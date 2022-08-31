using Contracts;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Collections.ObjectModel;
using System.Fabric;
using System.Fabric.Description;

namespace PostManagementService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class PostManagementService : StatefulService, IPostOperator
    {
        public PostManagementService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<long> InsertPost(PostData postData)
        {
            try
            {
                IReliableDictionary<long, PostData> postCollection = await StateManager.GetOrAddAsync<IReliableDictionary<long, PostData>>("postCollection");
                IReliableDictionary<string, long> postIdCounter = await StateManager.GetOrAddAsync<IReliableDictionary<string, long>>("postIdCounter");
                IReliableDictionary<string, IEnumerable<PostData>> userPostsCollection = await StateManager.GetOrAddAsync<IReliableDictionary<string, IEnumerable<PostData>>>("userPostsCollection");

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    postData.Id = await postIdCounter.AddOrUpdateAsync(tx, "IdCounter", 0, (key, value) => ++value);
                    await postCollection.AddOrUpdateAsync(tx, postData.Id, postData, (key, value) => value, new TimeSpan(0, 0, 4), CancellationToken.None);
                    await userPostsCollection.AddOrUpdateAsync(tx, postData.UserId, new List<PostData>() { postData }, (k, v) => { ((List<PostData>)v).Add(postData); return v; }, new TimeSpan(0, 0, 4), CancellationToken.None);

                    await tx.CommitAsync();
                    
                    ServiceEventSource.Current.ServiceMessage(Context, $"InsertPost - New post. Id: {postData.Id} User: {postData.UserId} Content: {postData.Content}");

                    return postData.Id;
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"InsertPost - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
                throw;
            }
        }

        public async Task<PostData> RetrievePost(long postId)
        {
            try
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
                    return new PostData();
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"RetrievePost - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
                throw;
            }
        }

        public async Task<IEnumerable<PostData>> RetrieveUserPosts(string userId)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"Retrieving post of user: {userId}");

                IReliableDictionary<string, IEnumerable<PostData>> userPostsCollection = await StateManager.GetOrAddAsync<IReliableDictionary<string, IEnumerable<PostData>>>("userPostsCollection");

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    ConditionalValue<IEnumerable<PostData>> postData = await userPostsCollection.TryGetValueAsync(tx, userId);
                    await tx.CommitAsync();

                    if (postData.HasValue)
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, $"Posts by user: {userId} retrieved. Count: {postData.Value.Count()}");
                        return postData.Value;
                    }

                    ServiceEventSource.Current.ServiceMessage(Context, $"Posts by user: {userId} could not be found.");
                    return new List<PostData>();
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"RetrieveUserPosts - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
                throw;
            }
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
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await DefineMetricsAndPoliciesAsync();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task DefineMetricsAndPoliciesAsync()
        {
            FabricClient fabricClient = new FabricClient();
            Uri serviceUri = new Uri("fabric:/Postit/PostManagementService");

            StatefulServiceUpdateDescription serviceUpdateDescription = new StatefulServiceUpdateDescription();

            StatefulServiceLoadMetricDescription serviceLoadMetricDescription = new StatefulServiceLoadMetricDescription
            {
                Name = "PostManagementService",
                PrimaryDefaultLoad = 0,
                SecondaryDefaultLoad = 0,
                Weight = ServiceLoadMetricWeight.High
            };

            if (serviceUpdateDescription.Metrics == null)
            {
                serviceUpdateDescription.Metrics = new Metrics();
            }

            serviceUpdateDescription.Metrics.Add(serviceLoadMetricDescription);

            await fabricClient.ServiceManager.UpdateServiceAsync(serviceUri, serviceUpdateDescription);
        }

        private class Metrics : KeyedCollection<string, ServiceLoadMetricDescription>
        {
            protected override string GetKeyForItem(ServiceLoadMetricDescription item)
            {
                return item.Name;
            }
        }
    }
}
