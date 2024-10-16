using MTCG.Classes;
using MTCG.Http;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTCG.BusinessLogic
{
    public class UserHandler
    {
        private readonly List<User> _users = new List<User>();

        public async Task<HttpResponse> RegisterUser(HttpRequest request)
        {
            var user = JsonConvert.DeserializeObject<User>(request.Body);
            if (_users.Any(u => u.Username == user.Username))
            {
                return new HttpResponse("409 Conflict", "{\"error\": \"User already exists\"}");
            }

            _users.Add(user);
            return new HttpResponse("201 Created", "{\"message\": \"User registered successfully\"}");
        }

        public async Task<HttpResponse> LoginUser(HttpRequest request)
        {
            var loginData = JsonConvert.DeserializeObject<User>(request.Body);
            var user = _users.FirstOrDefault(u => u.Username == loginData.Username && u.Password == loginData.Password);

            if (user == null)
            {
                return new HttpResponse("401 Unauthorized", "{\"error\": \"Invalid credentials\"}");
            }

            user.Token = $"{user.Username}-mtcgToken";
            return new HttpResponse("200 OK", JsonConvert.SerializeObject(new { token = user.Token }));
        }
    }
}
