using Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Security;

namespace ClientRazorAPI.Pages
{
    public class IndexModel : PageModel
    {
        private Uri _authenticatorServiceUri = new Uri("fabric:/Postit/AuthenticatorService");
        private int _partitionCount = 5;

        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string Msg { get; set; }

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            
        }

        public async Task OnPost()
        {           
            long partitionId = (long)Username[0] % _partitionCount;
            IAuthenticator authenticatorProxy = ServiceProxy.Create<IAuthenticator>(_authenticatorServiceUri, new ServicePartitionKey(partitionId));

            string sessionId = await authenticatorProxy.LoginUser(Username, Password);

            if (String.IsNullOrEmpty(sessionId))
            {
                ViewData["confirmation"] = $"{Username}, failed to login.";
            }
            else
            {
                ViewData["confirmation"] = $"{Username}, logged in.";
            }
        }
    }
}