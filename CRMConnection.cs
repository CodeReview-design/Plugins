using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Tooling.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Croatia.Integration.BLL
{
    public class CRMConnection
    {
        private IOrganizationService _orgService;
        public CrmServiceClient _connection;
        private string _connectionError;
        private string _connectionString;
        public HttpClient client;
        public HttpResponseMessage responseMessage;
        public string crmuri;

        #region Properties
        public IOrganizationService OrgService
        {
            get
            {
                RenewTokenIfRequired();
                if (!_connection.IsReady)
                    Reconnect();
                return _orgService;
            }
        }

        public string ConnectionError
        {
            get
            {
                return _connectionError;
            }
        }

        public bool IsReady
        {
            get
            {
                if (_connection == null)
                    return false;
                else
                    return _connection.IsReady;
            }
        }
        public bool IsReadyAPI
        {
            get
            {
                if (responseMessage == null)
                {
                    return false;
                }
                return responseMessage.IsSuccessStatusCode;
            }
        }
        public string UserName
        {
            get
            {
                if (IsReady)
                {
                    Guid userId = _connection.GetMyCrmUserId();
                    return OrgService.Retrieve("systemuser", userId, new Microsoft.Xrm.Sdk.Query.ColumnSet("domainname")).GetAttributeValue<string>("domainname");
                }
                return "";
            }
        }

        #endregion

        public JObject getOdataCRMResult(CRMConnection crmServer, string Funcserviceurl)
        {
            JObject results = new JObject();
            try
            {

                var responseMessages = crmServer.client.GetAsync(crmServer.crmuri + Funcserviceurl).Result;
                if (responseMessages.IsSuccessStatusCode)
                {
                    results = JsonConvert.DeserializeObject<JObject>(responseMessages.Content.ReadAsStringAsync().Result);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            return results;
        }

        private static Lazy<CRMConnection> lazy = null;
        public static CRMConnection ConnectionInstance(string connectionString)
        {
            if (lazy == null)
            {
                lazy = new Lazy<CRMConnection>(() => new CRMConnection(connectionString));
            }
            return lazy.Value;

        }
        public static CRMConnection ConnectionInstanceAPI(string uri, string username, string password, string domain)
        {
            if (lazy == null)
            {
                lazy = new Lazy<CRMConnection>(() => new CRMConnection(uri, username, password, domain));
            }
            return lazy.Value;

        }

        public CRMConnection(string connectionString)
        {
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                _connectionString = connectionString;

                _connection = new CrmServiceClient(connectionString);
                //_connection.OrganizationServiceProxy.Timeout = new TimeSpan(0, 15, 0);

                _orgService = (IOrganizationService)_connection.OrganizationWebProxyClient != null ? (IOrganizationService)_connection.OrganizationWebProxyClient : (IOrganizationService)_connection.OrganizationServiceProxy;

            }
            catch (Exception e)
            {
                _connectionError = e.ToString();
            }
        }
        public CRMConnection(string uri, string username, string password, string domain)
        {
            try
            {
                crmuri = uri;
                client = new HttpClient(new HttpClientHandler() { Credentials = new NetworkCredential(username, password, domain) }); client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")); client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
                client.DefaultRequestHeaders.Add("OData-Version", "4.0");
                responseMessage = client.GetAsync("WhoAmI").Result;

            }
            catch (Exception e)
            {
                _connectionError = e.ToString();
            }
        }

        private void Reconnect()
        {
            try
            {
                _connection = new CrmServiceClient(_connectionString);
                _orgService = (IOrganizationService)_connection.OrganizationWebProxyClient != null ? (IOrganizationService)_connection.OrganizationWebProxyClient : (IOrganizationService)_connection.OrganizationServiceProxy;
            }
            catch (Exception e)
            {
                _connectionError = e.ToString();
            }
        }

        public void CloseConnection()
        {
            try
            {
                _connection.Dispose();
            }
            catch (Exception e)
            {

            }
        }

        private void RenewTokenIfRequired()
        {
            try
            {
                if (_connection.OrganizationServiceProxy != null && _connection.OrganizationServiceProxy.SecurityTokenResponse != null &&
                    DateTime.UtcNow.AddMinutes(15) >= _connection.OrganizationServiceProxy.SecurityTokenResponse.Response.Lifetime.Expires)
                    _connection.OrganizationServiceProxy.Authenticate();
            }
            catch (Exception)
            {

            }
        }
    }
}
