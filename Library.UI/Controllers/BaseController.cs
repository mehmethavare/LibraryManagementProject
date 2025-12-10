using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Library.UI.Controllers
{
    public class BaseController : Controller
    {
        protected void AddJwt(HttpClient client)
        {
            var token = HttpContext.Session.GetString("jwt");

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}
