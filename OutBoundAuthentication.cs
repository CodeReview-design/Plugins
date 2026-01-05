using Croatia.Integration.Contracts.OutBoundAuthentication;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;

namespace Croatia.Integration.BLL.Common
{
    public static class OutBoundAuthentication
    {


        //public CRMConnection crmServer;
        //public IOrganizationService organizationService;
        //private readonly string _connectionName = "CRMDynamics";

        //public OutBoundAuthentication()
        //{
        //    crmServer = HelperMethods.ConnectToCRM(_connectionName);
        //    organizationService = crmServer.OrgService;
        //}

        public static string GetOutBoundAuthorization(CRMConnection crmServer)
        {
            var list = new List<KeyValuePair<string, string>>();
            OnlineIntegrationService onlineIntegration = new OnlineIntegrationService(crmServer, "ProductFactory");
            string requestURL = onlineIntegration.getAttributeValue("Authorization");

            string apiKey = ConfigurationManager.AppSettings["x-api-key"].ToString();


            string client_id = ConfigurationManager.AppSettings["client_id"].ToString();
            string client_secret = ConfigurationManager.AppSettings["client_secret"].ToString();
            string grant_type = ConfigurationManager.AppSettings["grant_type"].ToString();

            list.Add(new KeyValuePair<string, string>("client_id", client_id));
            list.Add(new KeyValuePair<string, string>("client_secret", client_secret));
            list.Add(new KeyValuePair<string, string>("grant_type", grant_type));

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            ServicePointManager.ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => { return true; };
            HttpResponseMessage response = client.PostAsync(requestURL, new FormUrlEncodedContent(list)).Result;
            if (response.IsSuccessStatusCode)
            {
                var responseBody = response.Content.ReadAsStringAsync().Result;
                var responseObj = JsonConvert.DeserializeObject<AuthenticationResponse>(responseBody);
                return responseObj.token_type + " " + responseObj.access_token;
            }
            return null;
        }
    }
}

