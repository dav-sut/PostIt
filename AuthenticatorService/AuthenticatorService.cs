using Contracts;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;
using System.Security.Cryptography;
using System.Text;

namespace AuthenticatorService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class AuthenticatorService : StatefulService, IAuthenticator
    {
        public AuthenticatorService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<string> LoginUser(string userName, string password)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: Trying to authenticate user: {userName}.");

                IReliableDictionary<string, string> userCredentials = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("userCredentialsCollection");
                IReliableDictionary<string, string> sessionData = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("sessionDataCollection");

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    string sessionID = String.Empty;
                    ConditionalValue<string> storedPassword = await userCredentials.TryGetValueAsync(tx, userName);

                    if (storedPassword.HasValue && String.Compare(password, storedPassword.Value) == 0)
                    {
                        sessionID = GetSessionId(userName);
                        await sessionData.AddOrUpdateAsync(tx, userName, sessionID, (k, v) => sessionID);

                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: User: {userName} sucessfully authenticated. Generated session: {sessionID}");
                    }
                    else
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: User: {userName} failed to authenticate.");
                    }

                    await tx.CommitAsync();
                    return sessionID;
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"LoginUser - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
                throw;
            }
        }

        public async Task<bool> LogoutUser(string userName)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: Trying to logout user: {userName}.");

                IReliableDictionary<string, string> userCredentials = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("userCredentialsCollection");
                IReliableDictionary<string, string> sessionData = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("sessionDataCollection");

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    bool isValidUsername = await userCredentials.ContainsKeyAsync(tx, userName);
                    ConditionalValue<string> storedSessionId = await sessionData.TryGetValueAsync(tx, userName, LockMode.Update);

                    if (isValidUsername)
                    {
                        await sessionData.TryRemoveAsync(tx, userName);

                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: User: {userName} sucessfully logged out.");

                        await tx.CommitAsync();
                        return true;
                    }
                    else
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: User: {userName} failed to logout.");

                        await tx.CommitAsync();
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"LogoutUser - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
                throw;
            }
        }

        public async Task<bool> RegisterNewUser(string userName, string password)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: Trying to create new user: {userName}.");

                IReliableDictionary<string, string> userCredentials = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("userCredentialsCollection");

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    bool userAlreadyExists = await userCredentials.ContainsKeyAsync(tx, userName, LockMode.Update);

                    if (!userAlreadyExists)
                    {
                        await userCredentials.AddOrUpdateAsync(tx, userName, password, (k, v) => v);
                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: User: {userName} sucessfully created.");

                        await tx.CommitAsync();
                        return true;
                    }
                    else
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: User: {userName} already exists.");

                        await tx.CommitAsync();
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"RegisterNewUser - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
                throw;
            }
        }

        public async Task<bool> IsSessionValid(string userName, string sessionId)
        {
            try
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: Checking whether user: {userName} has a valid session (session id: '{sessionId}').");

                IReliableDictionary<string, string> sessionData = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("sessionDataCollection");

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    ConditionalValue<string> storedSessionId = await sessionData.TryGetValueAsync(tx, userName);

                    if (storedSessionId.HasValue && String.Compare(sessionId, storedSessionId.Value) == 0)
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: Valid session for user: {userName}.");
                        return true;
                    }
                    else
                    {
                        ServiceEventSource.Current.ServiceMessage(Context, $"AuthenticatorService: Invalid session for user: {userName}.");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceMessage(Context, $"IsSessionValid - Caught exception {e}. Inner exception {e.InnerException}. Stacktrace: {e.StackTrace} Inner stack trace: {e.InnerException?.StackTrace}.");
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
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private string GetSessionId(string userName)
        {
            /*string encodingInput = userName + DateTime.Now.Ticks.ToString();
            SHA512 sha512 = SHA512.Create();

            UTF8Encoding encoding = new UTF8Encoding();
            byte[] hashCode = sha512.ComputeHash(encoding.GetBytes(encodingInput));

            return encoding.GetString(hashCode);*/

            return userName + DateTime.Now.Ticks.ToString();
        }
    }
}
