using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using GraphLib.Entity;
using GraphLib.Utils;
using LiteDB;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace GraphLib
{
    public class Graph
    {

        private string _clientId;

        private string _clientSecret;

        private string[] _scopes;

        private string _redirectUri;

        private AuthenticationResult _result;

        private IConfidentialClientApplication _confidentialClientApplication;


        public Graph(string clientId, string clientSecret, string redirectUri, string[] scopes)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _scopes = scopes;
            _redirectUri = redirectUri;
            _confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithRedirectUri(redirectUri)
                .WithClientSecret(clientSecret)
                .Build();
            _confidentialClientApplication.UserTokenCache.SetBeforeAccess(beforeAccess =>
            {

                if (LiteDbHelper.Instance.Any<TokenCacheEntity>(TableName.TokenTable, x => x.Name == clientId))
                {
                    beforeAccess.TokenCache.DeserializeMsalV3(LiteDbHelper.Instance.GetCollection<TokenCacheEntity>(TableName.TokenTable).FindOne(x => x.Name == clientId).Token);
                }
            });
            _confidentialClientApplication.UserTokenCache.SetAfterAccess(afterAccess =>
            {
                var tokenBytes = afterAccess.TokenCache.SerializeMsalV3();
                if (tokenBytes != null)
                {
                    var cache = LiteDbHelper.Instance.GetCollection<TokenCacheEntity>(TableName.TokenTable).FindOne(x => x.Name == clientId) ?? new TokenCacheEntity();
                    cache.Name = clientId;
                    cache.Token = tokenBytes;
                    LiteDbHelper.Instance.InsertOrUpdate(TableName.TokenTable, cache);
                }
            });
        }

        public async Task<GraphServiceClient> GetGraph()
        {
            if (_result == null || _result.ExpiresOn < DateTimeOffset.UtcNow)
            {
                var accounts = await _confidentialClientApplication.GetAccountsAsync();
                if (!accounts.Any())
                {
                    return null;
                }
                var result = await _confidentialClientApplication.AcquireTokenSilent(_scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
                if (result == null || string.IsNullOrEmpty(result.AccessToken) || result.ExpiresOn < DateTimeOffset.UtcNow)
                {
                    return null;
                }
                _result = result;
            }
            GraphServiceClient graphServiceClient =
                new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", _result.AccessToken);
                    return Task.CompletedTask;
                }));
            return graphServiceClient;
        }

        public async Task<GraphServiceClient> GetGraphWithCode(string code)
        {
            var result = await _confidentialClientApplication.AcquireTokenByAuthorizationCode(_scopes, code)
                .ExecuteAsync();
            if (result == null || string.IsNullOrEmpty(result.AccessToken) || result.ExpiresOn < DateTimeOffset.UtcNow)
            {
                return null;
            }
            _result = result;
            GraphServiceClient graphServiceClient =
                new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", _result.AccessToken);
                    return Task.CompletedTask;
                }));
            return graphServiceClient;
        }

        public string GetAuthUrl(string status = "status")
        {
            return $@"https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={_clientId}&response_type=code&redirect_uri={_redirectUri}&status={HttpUtility.UrlEncode(status)}&response_mode=form_post&scope={HttpUtility.UrlEncode(string.Join(" ", _scopes))}";
        }

        public static string GetCreateAppUrl(string appName, string redirectUri)
        {
            var ru = $"https://developer.microsoft.com/en-us/graph/quick-start?appID=_appId_&appName=_appName_&redirectUrl={redirectUri}&platform=option-dotnet";
            var deepLink =
                $"/quickstart/graphIO?publicClientSupport=false&appName={appName}&redirectUrl={redirectUri}&allowImplicitFlow=false&ru={HttpUtility.UrlEncode(ru)}";
            var appUrl = $"https://apps.dev.microsoft.com/?deepLink={HttpUtility.UrlEncode(deepLink)}";
            return appUrl;
        }
    }
}