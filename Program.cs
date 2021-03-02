using System;
using System.Net;
using System.Text.RegularExpressions;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ValorantAuth
{
    class Program
    {

        public static string AccessToken { get; set; }
        public static string EntitlementToken { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Enter Username: ");
            var username = Console.ReadLine();
            Console.WriteLine("Enter password: ");
            var password = Console.ReadLine();
            
            Login(username, password);
        }

        static void Login(string username, string password)
        {
            CookieContainer cookie = new CookieContainer();
            Authentication.GetAuthorization(cookie);

            var authJson = JsonConvert.DeserializeObject(Authentication.Authenticate(cookie, username, password));
            JToken authObj = JObject.FromObject(authJson);

            string authURL = authObj["response"]["parameters"]["uri"].Value<string>();
            var access_tokenVar = Regex.Match(authURL, @"access_token=(.+?)&scope=").Groups[1].Value;
            AccessToken = $"{access_tokenVar}";

            RestClient client = new RestClient(new Uri("https://entitlements.auth.riotgames.com/api/token/v1"));
            RestRequest request = new RestRequest(Method.POST);

            request.AddHeader("Authorization", $"Bearer {AccessToken}");
            request.AddJsonBody("{}");

            string response = client.Execute(request).Content;
            var entitlement_token = JsonConvert.DeserializeObject(response);
            JToken entitlement_tokenObj = JObject.FromObject(entitlement_token);

            EntitlementToken = entitlement_tokenObj["entitlements_token"].Value<string>();


            Console.WriteLine($"Entitlement Token: {EntitlementToken}");
            Console.WriteLine($"---------------------------------");
            Console.WriteLine($"Bearer Authentication Token: {AccessToken}");
        }
    }

    class Authentication
    {
        public static void GetAuthorization(CookieContainer jar)
        {
            string url = "https://auth.riotgames.com/api/v1/authorization";
            RestClient client = new RestClient(url);

            client.CookieContainer = jar;

            RestRequest request = new RestRequest(Method.POST);
            string body = "{\"client_id\":\"play-valorant-web-prod\",\"nonce\":\"1\",\"redirect_uri\":\"https://playvalorant.com/opt_in" + "\",\"response_type\":\"token id_token\",\"scope\":\"account openid\"}";
            request.AddJsonBody(body);
            client.Execute(request);
        }

        public static string Authenticate(CookieContainer cookie, string user, string pass)
        {
            string url = "https://auth.riotgames.com/api/v1/authorization";
            RestClient client = new RestClient(url);

            client.CookieContainer = cookie;

            RestRequest request = new RestRequest(Method.PUT);
            string body = "{\"type\":\"auth\",\"username\":\"" + user + "\",\"password\":\"" + pass + "\"}";
            request.AddJsonBody(body);

            return client.Execute(request).Content;
        }
    }
}
