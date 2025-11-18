using Croatia.CRM.Common.EntitiesEarlyBoundClasses;

using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
//using System.Text.Json;
using System.Xml.Serialization;

namespace Croatia.CRM.Common.Helpers
{
    public class Helper
    {
        //public dynamic fromJsonToDynamic(string json)
        //{
        //    dynamic jsonResponse = JsonSerializer.Deserialize<dynamic>(json);
        //    // dynamic jsonResponse = JsonConvert.DeserializeObject(json);
        //    return jsonResponse;

        //}
        public static string GetSystemPref(IOrganizationService _service, string s)
        {
            QueryExpression query = new QueryExpression()
            {
                EntityName = SystemPreference.EntityLogicalName,
                ColumnSet = new ColumnSet(new string[] { SystemPreference.Fields.Value }),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition(SystemPreference.Fields.Name, ConditionOperator.Equal, s);
            EntityCollection ec = _service.RetrieveMultiple(query);
            if (ec.Entities.Count < 1)
            {
                return null;
            }
            return ec.Entities[0][SystemPreference.Fields.Value].ToString();


        }
        public static string CustomerId(IOrganizationService org, Guid customerGuid)
        {
            Entity C = org.Retrieve(EntitiesEarlyBoundClasses.Contact.EntityLogicalName, customerGuid, new ColumnSet(new string[] { EntitiesEarlyBoundClasses.Contact.Fields.CustomerID }));
            return (string)C[EntitiesEarlyBoundClasses.Contact.Fields.CustomerID];

        }
        public static string Serialize<T>(T classObj) where T : class, new()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serialiaze = new DataContractJsonSerializer(typeof(T));
                serialiaze.WriteObject(ms, classObj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T DeSerialize<T>(string classObj) where T : class, new()
        {
            DataContractJsonSerializer deSerialiaze = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(classObj)))
            {
                return deSerialiaze.ReadObject(ms) as T;
            }

        }
        public static string SerializeObject(object obj, bool isJsonSerialize = false)
        {
            try
            {
                if (isJsonSerialize == true)
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(obj.GetType());
                        jsonSerializer.WriteObject(stream, obj);
                        stream.Position = 0;
                        StreamReader sr = new StreamReader(stream);
                        return sr.ReadToEnd();
                    }
                }
                using (StringWriter stream = new StringWriter())
                {
                    var serializer = new XmlSerializer(obj.GetType());
                    serializer.Serialize(stream, obj);
                    return stream.ToString();
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


        public static Entity GetEntityByCode(IOrganizationService _service, string entityName, string code, string fieldName = "cro_code")
        {
            QueryExpression query = new QueryExpression()
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet(false),
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition(fieldName, ConditionOperator.Equal, code);
            EntityCollection ec = _service.RetrieveMultiple(query);
            if (ec.Entities.Count < 1)
            {
                return null;
            }
            return ec.Entities[0];
            // return result;
        }
        public static ActionResponse StartAction(ITracingService tracing, IOrganizationService organizationService, string entityLogicalName, Guid recordId, Guid actionId)
        {
            tracing.Trace($"entityLogicalName = {entityLogicalName} & recordId = {recordId} & actionId = {actionId}");
            Entity processEntity = organizationService.Retrieve("workflow", actionId, new ColumnSet("uniquename"));

            if (processEntity.Attributes.Contains("uniquename"))
            {
                OrganizationRequest actionRequest = new OrganizationRequest($"cro_{processEntity["uniquename"]}");
                tracing.Trace("1");
                //actionRequest["inputdata"] = "hi"; ///what will be the input for the action
                actionRequest["Target"] = new EntityReference(entityLogicalName, recordId);
                bool isSuccess;
                string errormessage;
                try
                {
                    OrganizationResponse response = organizationService.Execute(actionRequest);
                    isSuccess = (bool)response.Results["issuccess"];
                    errormessage = response.Results["errormessage"].ToString();
                }
                catch (Exception ex)
                {
                    isSuccess = false;
                    errormessage = ex.Message;
                }
                tracing.Trace($"4  isSuccess = {isSuccess}");
                tracing.Trace($"5  errormessage = {errormessage}");
                ActionResponse actionResponse = new ActionResponse(isSuccess, errormessage);
                tracing.Trace("6");

                return actionResponse;
            }
            return null;
        }
        public static OrganizationResponse RunAction(IOrganizationService organizationService, OrganizationRequest actionRequest)
        {   
            try
            {
                OrganizationResponse response = organizationService.Execute(actionRequest);
                return response;
            }
            catch (Exception ex)
            {
                var inner = "";
                if (ex.InnerException != null) inner = ex.InnerException.ToString();
                throw new Exception($"Ex.Message ==> {ex.Message} and Inner message ==> {inner}");
            }
        }
        public static int getAccountStatus(string name)
        {
            switch (name)
            {
                case "CLOSED":
                    return (int)AccountStatus.CLOSED;
                case "UNCLAIMED":
                    return (int)AccountStatus.UNCLAIMED;
                case "AUTH":
                case "ACTIVE":
                    return (int)AccountStatus.ACTIVE;
                case "DORMANT":
                    return (int)AccountStatus.DORMANT;
                case "ABANDON":
                    return (int)AccountStatus.ABANDON;
                default:
                    return (int)AccountStatus.ACTIVE;
            }

        }
        public static string CreateJwt(IOrganizationService service, ITracingService tracing, JwtRequest jwt)
        {
            OnlineIntegrationService serviceParameters = new OnlineIntegrationService(service, "CreateJwtToken");
            string requestURL = serviceParameters.getAttributeValue("Internal URL");



            string reqJson = Helper.Serialize(jwt);

            var stringContent = new StringContent(reqJson, UnicodeEncoding.UTF8, "application/json");

            HttpClient httpClient = new HttpClient();

            var httpResponseMessage = httpClient.PostAsync(requestURL, stringContent).Result;
            tracing.Trace("create jwt status code: " + httpResponseMessage.StatusCode);

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                tracing.Trace("JWT generated successfully");
                JwtResponse jwtResponse = Helper.DeSerialize<JwtResponse>(httpResponseMessage.Content.ReadAsStringAsync().Result);
                string tokn = jwtResponse.jwt;
                return tokn;
            }
            return null;
        }
        public static JwtRequest GetJwtConfigrationIntegration(IOrganizationService service, ITracingService tracing, string Name)
        {
            JwtRequest jwt = new JwtRequest();
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cro_integrationconfiguration'>
    <attribute name='cro_integrationconfigurationid'/>
    <attribute name='cro_name'/>
    <attribute name='cro_username'/>
    <attribute name='cro_channelid'/>
    <order attribute='cro_name' descending='false'/>
    <filter type='and'>
      <condition attribute='cro_name' operator='eq' value='" + Name + @"'/>
    </filter>
  </entity>
</fetch>";
            EntityCollection res = service.RetrieveMultiple(new FetchExpression(Fetch));
            if (res != null && res.Entities != null && res.Entities.Count > 0)
            {
                Entity IntegrationConfigration = res.Entities[0];
                if (IntegrationConfigration.Contains(Common.EntitiesEarlyBoundClasses.IntegrationConfiguration.Fields.ChannelID) && IntegrationConfigration.Contains(Common.EntitiesEarlyBoundClasses.IntegrationConfiguration.Fields.UserName))
                {
                    jwt.UserName = IntegrationConfigration[Common.EntitiesEarlyBoundClasses.IntegrationConfiguration.Fields.UserName].ToString();
                    jwt.channelId = IntegrationConfigration[Common.EntitiesEarlyBoundClasses.IntegrationConfiguration.Fields.ChannelID].ToString();
                    return jwt;
                }
            }
            return null;
        }
        public static Entity RetrieveLovMappingByCode(IOrganizationService organizationService, string code, string entityname, string fieldName)
        {
            Entity LovMapping = null;
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='cro_lovmapping'>
                                <attribute name='cro_lovmappingid' />
                                <attribute name='cro_name' />
                                <attribute name='cro_type' />
                                <attribute name='statuscode' />
                                <attribute name='statecode' />
                                <attribute name='cro_fieldtype' />
                                <attribute name='cro_entityname' />
                                <attribute name='cro_codefieldname' />
                                <attribute name='cro_code' />
                                <attribute name='cro_mappedcode' />
                                <order attribute='cro_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <condition attribute='cro_entityname' operator='eq' value='" + entityname + @"' />
                                  <condition attribute='cro_codefieldname' operator='eq' value='" + fieldName + @"' />
                                  <condition attribute='cro_code' operator='eq' value='" + code + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            var result = organizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                LovMapping = result.Entities[0];
            }
            return LovMapping;
        }

        public enum AccountStatus
        {
            CLOSED = 1,
            UNCLAIMED = 171080000,
            ACTIVE = 171080001,
            DORMANT = 171080002,
            ABANDON = 171080003,
            INACTIVE = 171080004,
        };
        public static Entity GetTransactionBy(ITracingService tracingService, IOrganizationService orgService, string transactionId, Guid Account)
        {
            Entity transaction = null;
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                          <entity name='cro_transaction'>
                            <attribute name='cro_transactionid' />
                            <attribute name='cro_transactionnumber' />
                            <attribute name='createdon' />
                            <order attribute='cro_transactionnumber' descending='false' />
                            <filter type='and'>
                              <condition attribute='cro_account' operator='eq'  value='" + Account + @"' />
                              <condition attribute='cro_transactionnumber' operator='eq' value='" + transactionId + @"' />
                            </filter>
                          </entity>
                        </fetch>";
            var results = orgService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (results != null && results.Entities != null && results.Entities.Count > 0)
            {
                transaction = results.Entities[0];
                tracingService.Trace("transaction --> " + transaction["cro_transactionid"].ToString());
            }
            return transaction;
        }

        #region Assign case opertation

        ///<Author>M-elazizi</Author>
        ///<Author>shrouk</Author>
        /// <summary>
        /// this method to assign case to user, team or queue based on the assignTo optionSet value
        /// </summary>
        /// <param name="ConfigEntity"></param>
        /// <param name="ConfigEntityName"></param>
        /// <param name="TargetEntity"></param>
        /// <param name="TargetEntityId"></param>
        /// <param name="currentUser"></param>
        /// <param name="orgService"></param>
        /// <param name="tracing"></param>
        public static void Assign(Entity ConfigEntity, String ConfigEntityName, Entity TargetEntity, Guid TargetEntityId, EntityReference currentUser, IOrganizationService orgService, ITracingService tracing)
        {
            tracing.Trace($"1 --> start AssignCase()");
            var assigntoSchema = "";
            var userSchema = "";
            var teamSchema = "";
            var queueSchema = "";
            var target = new EntityReference();
            Entity updateIncidentOrTask =null;
            if (ConfigEntityName == Classification.EntityLogicalName)
            {
                assigntoSchema = Classification.Fields.AssignTo;
                userSchema = Classification.Fields.User;
                teamSchema = Classification.Fields.Team;
                queueSchema = Classification.Fields.Queue;
                target = new EntityReference(Case.EntityLogicalName, TargetEntityId);
                updateIncidentOrTask = new Entity(Case.EntityLogicalName, TargetEntityId);
                tracing.Trace($"Classification TargetEntity.Id --> {TargetEntityId}");
            }
            else if (ConfigEntityName == TaskConfiguration.EntityLogicalName)
            {
                assigntoSchema = TaskConfiguration.Fields.AssignTo;
                userSchema = TaskConfiguration.Fields.User;
                teamSchema = TaskConfiguration.Fields.Team;
                queueSchema = TaskConfiguration.Fields.Queue;
                target = new EntityReference(Task.EntityLogicalName, TargetEntityId);
                updateIncidentOrTask = new Entity(Task.EntityLogicalName, TargetEntityId);
                tracing.Trace($"TaskConfiguration TargetEntityId --> {TargetEntityId}");
            }

            if (ConfigEntity.Contains(assigntoSchema))
            {
                Guid targetUser = currentUser.Id;
                string assignUser = User.EntityLogicalName;
                var assigntoValue = (int)GlobalEnums.AssignTo.OwningUser;
                if (ConfigEntity.Contains(assigntoSchema))
                    assigntoValue = ((OptionSetValue)ConfigEntity.Attributes[assigntoSchema]).Value;

                tracing.Trace($"2 --> assign to = {assigntoValue}");
                tracing.Trace($"3 --> user = {currentUser.Name}");
                tracing.Trace($"4 --> user id = {currentUser.Id}");
                switch (assigntoValue)
                {
                    case (int)GlobalEnums.AssignTo.OwningUser:
                        targetUser = currentUser.Id;
                        tracing.Trace($"5 --> AssignTo.OwningUser");
                        break;
                    case (int)GlobalEnums.AssignTo.User:
                        if (ConfigEntity.Contains(userSchema))
                        {
                            targetUser = ((EntityReference)ConfigEntity[userSchema]).Id;
                            tracing.Trace($"6 --> AssignTo.User = {((EntityReference)ConfigEntity[userSchema]).Name}");
                        }
                        break;
                    case (int)GlobalEnums.AssignTo.Team:
                        if (ConfigEntity.Contains(teamSchema))
                        {
                            assignUser = Team.EntityLogicalName;
                            targetUser = ((EntityReference)ConfigEntity[teamSchema]).Id;
                            tracing.Trace($"7 --> AssignTo.team = {((EntityReference)ConfigEntity[teamSchema]).Name}");
                        }
                        break;
                    case (int)GlobalEnums.AssignTo.Queue:
                             tracing.Trace($"AssignTo.Queue start create queueitem");
                        try
                        {
                            var queueItem = new Entity("queueitem");
                            queueItem["objectid"] = target;
                            queueItem["queueid"] = (EntityReference)ConfigEntity[queueSchema];
                            tracing.Trace($"QueueItem Details --> objectid: {target.Id}, queueid: {((EntityReference)ConfigEntity[queueSchema]).Id}");
                            Guid queueItemID = orgService.Create(queueItem);
                            tracing.Trace($"AssignTo.Queue created queueitem --> {queueItemID}");

                            Entity queue = orgService.Retrieve(EntitiesEarlyBoundClasses.Queue.EntityLogicalName, ((EntityReference)ConfigEntity[queueSchema]).Id, new ColumnSet("ownerid"));
                            if (queue.Contains("ownerid"))
                            {
                                targetUser = ((EntityReference)queue["ownerid"]).Id;
                                assignUser = ((EntityReference)queue["ownerid"]).LogicalName;
                                tracing.Trace($"8 --> AssignTo.Queue.type.logicalName = {assignUser}");
                            }
                            tracing.Trace($"8 --> AssignTo.Queue = {((EntityReference)ConfigEntity[queueSchema]).Name}");
                            tracing.Trace($"9 -->assign to queue owner = {((EntityReference)queue["ownerid"]).Name}");
                        }
                        catch (Exception ex)
                        {
                            tracing.Trace($"Error during queueitem creation: {ex.Message}");         
                        }
                        tracing.Trace($"AssignTo.Queue end create queueitem");
                        break;
                    default:
                        targetUser = currentUser.Id;
                        tracing.Trace($"10 --> default {currentUser.Name}");
                        break;
                }
                if (targetUser != currentUser.Id)
                {
                    //AssignRequest assignRequest = new AssignRequest
                    //{
                    //    Assignee = new EntityReference(assignUser, targetUser),
                    //    Target = target
                    //};
                    try
                    {
                        tracing.Trace($"1 --> before assign ");
                        tracing.Trace($"Updating ownerid to EntityLogicalName: {assignUser}, targetUser: {targetUser}");
                        updateIncidentOrTask["ownerid"] = new EntityReference(assignUser, targetUser);
                    //orgService.Execute(assignRequest);
                   
                    if (ConfigEntityName == Classification.EntityLogicalName&&ConfigEntity.Contains(Classification.Fields.AssignTo))
                    {
                        var assignTo = ConfigEntity.GetAttributeValue<OptionSetValue>(Classification.Fields.AssignTo).Value;
                        if (assignTo == 4)
                        {
                            EntityReference classificationQueue = ConfigEntity.GetAttributeValue<EntityReference>(Classification.Fields.Queue);
                            tracing.Trace($"Updating AssignedQueue to queue id: {classificationQueue.Id}");
                            updateIncidentOrTask[Case.Fields.AssignedQueue] = new EntityReference(EntitiesEarlyBoundClasses.Queue.EntityLogicalName, classificationQueue.Id);
                        }
                    }
                    orgService.Update(updateIncidentOrTask);
                    tracing.Trace($"12 --> after assign ");
                    }
                    catch (Exception ex)
                    {
                        tracing.Trace($"Error during entity update: {ex.Message}");
                       
                    }
                }
            }

        }


        #endregion

        #region Duplicated Cases

        #region activeCaseWithSameClassification
        public static DuplicatedCaseResponse activeCaseWithSameClassification(ITracingService tracingService, IOrganizationService organizationService, Entity Case, Guid Customer, Guid caseType, Guid product, Guid subProduct, Guid process, string apply_duplicate_check_on_cases_with_status = "")
        {
            EntityCollection additionalAttributes = Helper.GetProcessAdditionalAttributes(organizationService, process);

            List<string> schemas = new List<string>();
            List<int> types = new List<int>();
            if (additionalAttributes != null && additionalAttributes.Entities != null && additionalAttributes.Entities.Count > 0)
            {
                foreach (var additionalAttribute in additionalAttributes.Entities)
                {
                    if (additionalAttribute.Contains(AdditionalAttribute.Fields.IsDuplicationParameter) && ((bool)additionalAttribute.Attributes[AdditionalAttribute.Fields.IsDuplicationParameter]))
                    {
                        if (additionalAttribute.Contains(AdditionalAttribute.Fields.SchemaName_cro_name))
                        {
                            schemas.Add(additionalAttribute.Attributes[AdditionalAttribute.Fields.SchemaName_cro_name].ToString());
                            tracingService.Trace("additional" + additionalAttribute.Attributes[AdditionalAttribute.Fields.SchemaName_cro_name].ToString());
                        }
                        if (additionalAttribute.Contains(AdditionalAttribute.Fields.Type))
                        {
                            types.Add((int)((OptionSetValue)additionalAttribute.Attributes[AdditionalAttribute.Fields.Type]).Value);
                            tracingService.Trace("type" + (int)((OptionSetValue)additionalAttribute.Attributes[AdditionalAttribute.Fields.Type]).Value);
                        }
                    }
                }
            }
            DuplicatedCaseResponse response = new DuplicatedCaseResponse();
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='incident'>
    <attribute name='title' />
    <attribute name='ticketnumber' />
    <attribute name='createdon' />
    <attribute name='incidentid' />
    <attribute name='caseorigincode' />
    <attribute name='statuscode' />
    <order attribute='title' descending='false' />
    <filter type='and'>";
            if (Customer != null || Customer != Guid.Empty)
            {
                fetchXml += @"<condition attribute='customerid' operator='eq' value='" + Customer + @"' />";
            }
            /*if (classification != null || classification != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_classificationcodelookup' operator='eq' value='" + classification + @"' />";
            }*/
            if (caseType != null || caseType != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_casetype' operator='eq' value='" + caseType + @"' />";
            }
            if (product != null || product != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_productlookup' operator='eq' value='" + product + @"' />";
            }
            if (subProduct != null || subProduct != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_subproductlookup' operator='eq' value='" + subProduct + @"' />";
            }
            if (process != null || process != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_processlookup' operator='eq' value='" + process + @"' />";
            }
            if (schemas != null)
            {
                for (int i = 0; i < schemas.Count; i++)
                {
                    if (Case.Contains(schemas[i]))
                    {
                        switch (types[i])
                        {
                            case ((int)AdditionalAttribute.TypeEnum.SingleLineofText):
                                {
                                    fetchXml += @"<condition attribute='" + schemas[i] + @"' operator='eq' value='" + Case.Attributes[schemas[i]].ToString() + @"' />";
                                    break;
                                }
                            case ((int)AdditionalAttribute.TypeEnum.Lookup):
                                {
                                    fetchXml += @"<condition attribute='" + schemas[i] + @"' operator='eq' value='" + ((EntityReference)Case.Attributes[schemas[i]]).Id + @"' />";
                                    break;
                                }
                            case ((int)AdditionalAttribute.TypeEnum.OptionSet):
                                {
                                    fetchXml += @"<condition attribute='" + schemas[i] + @"' operator='eq' value='" + (int)((OptionSetValue)Case.Attributes[schemas[i]]).Value + @"' />";
                                    break;
                                }
                            case ((int)AdditionalAttribute.TypeEnum.WholeNumber):
                                {
                                    fetchXml += @"<condition attribute='" + schemas[i] + @"' operator='eq' value='" + (int)Case.Attributes[schemas[i]] + @"' />";
                                    break;
                                }
                            case ((int)AdditionalAttribute.TypeEnum.DecimalNumber):
                                {
                                    fetchXml += @"<condition attribute='" + schemas[i] + @"' operator='eq' value='" + (decimal)Case.Attributes[schemas[i]] + @"' />";
                                    break;
                                }
                            case ((int)AdditionalAttribute.TypeEnum.DateandTime):
                                {
                                    fetchXml += @"<condition attribute='" + schemas[i] + @"' operator='eq' value='" + (DateTime)Case.Attributes[schemas[i]] + @"' />";
                                    break;
                                }
                        }
                    }
                }

            }

            // At the end of your filters, before closing </filter>
            if (!string.IsNullOrEmpty(apply_duplicate_check_on_cases_with_status))
            {
                string[] statusValues = apply_duplicate_check_on_cases_with_status.Split(',');

                fetchXml += "<filter type='or'>";
                foreach (var status in statusValues)
                {
                    fetchXml += $"<condition attribute='statuscode' operator='eq' value='{status}' />";
                }
                fetchXml += "</filter>";
            }
            else
            {
                fetchXml += @"<condition attribute='statecode' operator='eq' value='0' />";
            }

            fetchXml += @"
    </filter>
  </entity>
</fetch>";
            var result = organizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                response.ActiveCaseWithSameClassification = true;
                response.ExisitingCaseNumber = result.Entities[0].Attributes[EntitiesEarlyBoundClasses.Case.Fields.CaseNumber].ToString();
                response.status = result.Entities[0].GetAttributeValue<OptionSetValue>("statuscode")?.Value ?? -1;
            }
            return response;
        }
        #endregion
        #region Get Classification Additional Attributies by Process
        
        public static EntityCollection GetProcessAdditionalAttributes(IOrganizationService organizationService, Guid processGuid)
        {
            //Guid AttributesConfigGuid = Guid.Empty;
            string fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cro_additionalattribute'>
    <attribute name='cro_additionalattributeid' />
    <attribute name='cro_name' />
    <attribute name='cro_type' />
    <attribute name='cro_lookupentityname' />
    <attribute name='cro_mappedfield' />
    <attribute name='cro_isduplicationparameter' />
    <attribute name='cro_displayname' />
    <link-entity name='cro_additionalattributesconfig' from='cro_additionalattributesconfigid' to='cro_additionalattributesconfig' link-type='inner' alias='ab'>
        <filter type='and'>
        <condition attribute='cro_process' operator='eq' uitype='cro_process' value='"+processGuid+@"' />
        </filter>
    </link-entity>
  </entity>
</fetch>";
             return organizationService.RetrieveMultiple(new FetchExpression(fetch));
            
        }
        
        #endregion
        #region classificationAllowDuplicated
        public static bool classificationAllowDuplicated(IOrganizationService organizationService, Guid caseType, Guid product, Guid subProduct, Guid process, ref string apply_duplicate_check_on_cases_with_status, ref string Code)
        {
            bool allowDuplicated = false;
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cro_classification'>
    <attribute name='cro_classificationid' />
    <attribute name='cro_classificationcode' />
    <attribute name='cro_name' />
    <attribute name='createdon' />
    <attribute name='cro_allowmultiplecasecreation' />
    <attribute name='cro_applyduplicatecheckoncaseswithstatus' />
    <order attribute='cro_name' descending='false' />
    <filter type='and'>";
            if (caseType != null || caseType != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_casetype' operator='eq' value='" + caseType + @"' />";
            }
            if (product != null || product != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_product' operator='eq' value='" + product + @"' />";
            }
            if (subProduct != null || subProduct != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_subproduct' operator='eq' value='" + subProduct + @"' />";
            }
            if (process != null || process != Guid.Empty)
            {
                fetchXml += @"<condition attribute='cro_process' operator='eq' value='" + process + @"' />";
            }

            fetchXml += @"</filter>
  </entity>
</fetch>";
            var result = organizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                if (result.Entities[0].Contains(EntitiesEarlyBoundClasses.Classification.Fields.ClassificationCode))
                {
                    Code = result.Entities[0].Attributes[EntitiesEarlyBoundClasses.Classification.Fields.ClassificationCode].ToString();
                }
                if (result.Entities[0].Contains(EntitiesEarlyBoundClasses.Classification.Fields.AllowMultipleCaseCreation))
                {
                    allowDuplicated = (bool)result.Entities[0].Attributes[EntitiesEarlyBoundClasses.Classification.Fields.AllowMultipleCaseCreation];
                }
                if (result.Entities[0].Contains("cro_applyduplicatecheckoncaseswithstatus"))
                {
                    OptionSetValueCollection selectedValues = (OptionSetValueCollection)result.Entities[0].Attributes["cro_applyduplicatecheckoncaseswithstatus"];
                    foreach (var option in selectedValues)
                    {
                        apply_duplicate_check_on_cases_with_status = string.Join(",", selectedValues.Select(x => x.Value));
                    }
                }
            }
            return allowDuplicated;
        }
        #endregion
        #region MElAZIZI BPF processes
        public struct ProcessStage
        {
            public static Guid ResolveGuid => new Guid("356ecd08-43c3-4585-ae94-6053984bc0a9");
            public static Guid CaseReviewGuid => new Guid("a89738f9-487b-4d58-88ba-3b0faa4d04f1");
            public static Guid IdentifyGuid => new Guid("15322a8f-67b8-47fb-8763-13a28686c29d");
        }
        public static Guid getphoneToCaseProcessIdByCaseId(Guid id, IOrganizationService orgService, ITracingService tracing)
        {
            string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
            <entity name='phonetocaseprocess'>
            <attribute name='businessprocessflowinstanceid' />
            <filter type='and'>
                <condition attribute='incidentid' operator='eq' value='" + id + @"' />
            </filter>
            </entity>
            </fetch>";
            tracing.Trace("UpdateBPF --> getphoneToCaseProcessIdByCaseId ----> start");
            var result = orgService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                tracing.Trace("UpdateBPF --> getphoneToCaseProcessIdByCaseId ----> result----> " + result.Entities.ToString());
                tracing.Trace("UpdateBPF --> getphoneToCaseProcessIdByCaseId ----> result.Entities[0].Attributes[businessprocessflowinstanceid]----> " + result.Entities[0].Attributes["businessprocessflowinstanceid"].ToString());
                return new Guid(result.Entities[0].Attributes["businessprocessflowinstanceid"].ToString());
            }
            return Guid.Empty;
        }
        public static void UpdateBPF(Guid id, Guid processStageId, int stageState, int caseState, IOrganizationService orgService, ITracingService tracing)
        {
            tracing.Trace("start UpdateBPF");
            tracing.Trace("UpdateBPF --> case id --> " + id);
            tracing.Trace("UpdateBPF --> processStageId --> " + stageState);
            tracing.Trace("UpdateBPF --> processStageId --> " + caseState);

            #region retrive phone to case process 
            Guid phoneToCaseProcessId = Helper.getphoneToCaseProcessIdByCaseId(id, orgService, tracing);
            tracing.Trace("UpdateBPF --> phoneToCaseProcessId --> " + phoneToCaseProcessId);
            #endregion
            Entity record = new Entity("phonetocaseprocess", phoneToCaseProcessId);
            record["statecode"] = new OptionSetValue(stageState); // State
            record["statuscode"] = new OptionSetValue(caseState); // Status
            record["activestageid"] = new EntityReference("processstage", processStageId); // resolve statge
            orgService.Update(record);
            tracing.Trace("end UpdateBPF");
        }
        #endregion
        #region Get Addtional Attributes
        public static List<Entity> GetAdditionalAttributesbyConfig(IOrganizationService service, Guid additionalAttrConfigGuid)
        {
            List<Entity> Attributes = new List<Entity>();
            string fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cro_additionalattribute'>
    <attribute name='cro_additionalattributeid' />
    <attribute name='cro_name' />
    <attribute name='cro_type' />
    <attribute name='cro_lookupentityname' />
    <attribute name='cro_mappedfield' />
    <attribute name='cro_isduplicationparameter' />
    <attribute name='cro_displayname' />
    <filter type='and'>
      <condition attribute='cro_additionalattributesconfig' operator='eq' value='" + additionalAttrConfigGuid + @"' />
    </filter>
  </entity>
</fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetch));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                Entity addtionalAttributes = new Entity();
                for (int i = 0; i < result.Entities.Count; i++)
                {
                    addtionalAttributes = result.Entities[i];
                    Attributes.Add(addtionalAttributes);
                }
            }
            return Attributes;

        }
        public static List<Entity> GetAdditionalAttributesProcess(IOrganizationService service, Guid processGuid)
        {
            Guid AttributesConfigGuid = new Guid();
            string fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cro_additionalattributesconfig'>
    <attribute name='cro_additionalattributesconfigid' />
    <attribute name='cro_name' />
    <filter type='and'>
      <condition attribute='cro_process' operator='eq' value='" + processGuid + @"' />
    </filter>
  </entity>
</fetch>";
            var result = service.RetrieveMultiple(new FetchExpression(fetch));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {

                AttributesConfigGuid = result.Entities[0].Id;
            }
            List<Entity> attributes = GetAdditionalAttributesbyConfig(service, AttributesConfigGuid);
            return attributes;

        }

        #endregion

        public static Entity GetIncidentDetailsBySamaRefCode(IOrganizationService orgService, ITracingService tracingService, string samaRefNumber)
        {
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='incident'>
                                <attribute name='incidentid' />
                                <attribute name='ticketnumber' />
                                <attribute name='prioritycode' />
                                <attribute name='title' />
                                <attribute name='customerid' />
                                <attribute name='ownerid' />
                                <attribute name='statuscode' />
                                <attribute name='statecode' />
                                <attribute name='createdon' />
                                <attribute name='caseorigincode' />
                                <attribute name='cro_unrecognizedcustomer' />
                                <attribute name='cro_samareferencenumber' />
                                <attribute name='cro_additional_samareferenceno' />
                                <attribute name='cro_channel' />
                                <attribute name='cro_casetype' />
                                <attribute name='cro_subproductlookup' />
                                <attribute name='cro_productlookup' />
                                <attribute name='cro_processlookup' />
                                <order attribute='title' descending='false' />
                                <filter type='and'>
                                  <condition attribute='cro_samareferencenumber' operator='eq' value='" + samaRefNumber + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetIncidentDetailsBySamaRefCode : " + res.Entities.Count);
            return res != null && res.Entities != null && res.Entities.Count > 0 ? res.Entities[0] : null;
        }

        public static Entity GetClassificationByCode(IOrganizationService orgService, ITracingService tracingService, string classificationCode)
        {
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
              <entity name='cro_classification'>
                <attribute name='cro_resolveenable'/>
                <attribute name='cro_reopenenable'/>
                <attribute name='cro_approveenable'/>
                <attribute name='cro_rejectenable'/>
                <attribute name='cro_incompleteenable'/>
                <attribute name='cro_assignto'/>
                <attribute name='cro_user'/>
                <attribute name='cro_team'/>
                <attribute name='cro_queue'/>
                <attribute name='cro_mobilecase'/>
                <attribute name='cro_ismobile'/>
                <attribute name='cro_priority'/>
                <attribute name='cro_classificationid'/>
                <attribute name='cro_name'/>
                <attribute name='cro_subproduct'/>
                <attribute name='cro_product'/>
                <attribute name='cro_classificationcode'/>
                <attribute name='cro_process'/>
                <attribute name='cro_casetype'/>
                <filter type='and'>
                  <condition attribute='statecode' operator='eq' value='0' />  
                  <condition attribute='cro_classificationcode' operator='eq' value='" + classificationCode + @"'/>
                </filter>
              </entity>
              </fetch>";
            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetClassificationByCode : " + res.Entities.Count);
            return res != null && res.Entities != null && res.Entities.Count > 0 ? res.Entities[0] : null;
        }
        public static Entity GetContactByIdNumber(IOrganizationService orgService, ITracingService tracingService, string idNumber)
        {
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                <attribute name='fullname' />
                                <attribute name='emailaddress1' />
                                <attribute name='cro_customerid' />
                                <attribute name='statuscode' />
                                <attribute name='cro_onboardingdate' />
                                <attribute name='mobilephone' />
                                <attribute name='createdon' />
                                <attribute name='cro_civilianid' />
                                <attribute name='contactid' />
                                <order attribute='fullname' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <condition attribute='cro_civilianid' operator='eq' value='" + idNumber + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetContactByIdNumber : " + res.Entities.Count);
            return res != null && res.Entities != null && res.Entities.Count > 0 ? res.Entities[0] : null;
        }
        public static Entity GetProspectRecord(IOrganizationService orgService, ITracingService tracingService)
        {
            tracingService.Trace("res. GetProspectRecord : stared");
            string prospectCustomerNumber = GetSystemPreferenceByName(orgService, tracingService, "ProspectCustomerNumber");
            if (prospectCustomerNumber != string.Empty)
            {
                string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='contact'>
                                <attribute name='fullname' />
                                <attribute name='emailaddress1' />
                                <attribute name='cro_customerid' />
                                <attribute name='statuscode' />
                                <attribute name='cro_onboardingdate' />
                                <attribute name='mobilephone' />
                                <attribute name='createdon' />
                                <attribute name='cro_civilianid' />
                                <attribute name='contactid' />
                                <order attribute='fullname' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <condition attribute='cro_customerid' operator='eq' value='" + prospectCustomerNumber + @"' />
                                </filter></entity></fetch> ";
                tracingService.Trace("res. GetProspectRecord Fetch : " + Fetch);
                EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
                tracingService.Trace("res. GetProspectRecord : " + res.Entities.Count);
                return res != null && res.Entities != null && res.Entities.Count > 0 ? res.Entities[0] : null;
            }
            return null;
        }
        public static Entity GetLeadByMobilePhoneNumber(IOrganizationService orgService, ITracingService tracingService, string mobileNumber)
        {
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='lead'>
                                <attribute name='fullname' />
                                <attribute name='createdon' />
                                <attribute name='leadid' />
                                <attribute name='mobilephone' />
                                <order attribute='createdon' descending='true' />
                                <filter type='and'>
                                  <condition attribute ='mobilephone' operator= 'eq' value = '" + mobileNumber + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            //
            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetLeadByMobilePhoneNumber : " + res.Entities.Count);
            return res != null && res.Entities != null && res.Entities.Count > 0 ? res.Entities[0] : null;
        }

        public static Entity GetLeadByIdNumber(IOrganizationService orgService, ITracingService tracingService, string idNumber)
        {
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='lead'>
                                <attribute name='fullname' />
                                <attribute name='createdon' />
                                <attribute name='leadid' />
                                <attribute name='mobilephone' />
                                <attribute name='cro_idtype' />
                                <order attribute='createdon' descending='true' />
                                <filter type='and'>
                                  <condition attribute ='cro_idnumber' operator= 'eq' value = '" + idNumber + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            //
            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetLeadByIdNumber : " + res.Entities.Count);
            return res != null && res.Entities != null && res.Entities.Count > 0 ? res.Entities[0] : null;
        }

        public static Entity GetIncidentDetailsById(IOrganizationService orgService, Guid incidentId, ColumnSet columnName)
        {
            Entity incidentDetails = orgService.Retrieve(Case.EntityLogicalName, incidentId, columnName);
            if (incidentDetails != null)
                return incidentDetails;
            return null;
        }
        public static string GetUserDefaultLanguage(IOrganizationService orgService, Guid userId)
        {
            string LanguageCode = "1033";
            Entity userSettings = orgService.Retrieve("usersettings", userId, new ColumnSet("uilanguageid"));
            if (userSettings != null)
                return Convert.ToString(userSettings["uilanguageid"]).Trim();
            return LanguageCode;
        }

        public static string GetSystemPreferenceByName(IOrganizationService orgService, ITracingService tracingService, string name)
        {
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='cro_systempreference'>
                                <attribute name='cro_systempreferenceid' />
                                <attribute name='cro_name' />
                                <attribute name='cro_value' />
                                <order attribute='cro_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <condition attribute='cro_name' operator='eq' value='" + name + @"' />
                                </filter>
                              </entity>
                            </fetch>";

            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetSystemPreferenceByName : " + res.Entities.Count);
            if (res.Entities != null && res.Entities.Count > 0)
            {
                var getSystemPreferenceEntity = res.Entities[0];
                return getSystemPreferenceEntity.Contains("cro_value") ? Convert.ToString(getSystemPreferenceEntity["cro_value"]).Trim() : string.Empty;
            }
            return string.Empty;
        }
        public static string GetcontactNumberByCivilian(IOrganizationService orgService, ITracingService tracingService, string civilianid)
        {
            string Fetch = @"<fetch>
	                            <entity name='contact'>
		                            <attribute name='contactid' />
		                            <attribute name='cro_customerid' />
		                            <filter type='and'>
			                            <condition attribute='cro_civilianid' operator='eq' value='" + civilianid + @"' />
		                            </filter>
	                            </entity>
                            </fetch>";

            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            if (res.Entities != null && res.Entities.Count > 0)
            {
                var contact = res.Entities[0];
                return contact.Contains("cro_customerid") ? contact["cro_customerid"].ToString() : null;
            }
            return null;
        }
        public static DataCollection<Entity> GetDocsConfigByClassificationID(ITracingService tracingService, IOrganizationService service, Guid ClassificationId)
        {

            DataCollection<Entity> DocsConfig = null;
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='true'>
              <entity name='cro_documentconfiguration'>
                <attribute name='cro_documentconfigurationid' />
                <attribute name='cro_name' />
                <attribute name='cro_arabicname' />
                <attribute name='cro_description' />
                <attribute name='cro_arabicdescription' />
                <attribute name='createdon' />
                <attribute name='cro_isrequired' />
                <order attribute='cro_name' descending='false' />
                <link-entity name='cro_cro_documentconfiguration_cro_classific' from='cro_documentconfigurationid' to='cro_documentconfigurationid' visible='false' intersect='true'>
                  <link-entity name='cro_classification' from='cro_classificationid' to='cro_classificationid' alias='ac'>
                    <filter type='and'>
                      <condition attribute='cro_classificationid' operator='eq' value='" + ClassificationId + @"' />
                    </filter>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>";
            EntityCollection res = service.RetrieveMultiple(new FetchExpression(fetchXml));
            tracingService.Trace("res." + res.Entities.Count);

            if (res != null && res.Entities != null && res.Entities.Count > 0)
            {
                DocsConfig = res.Entities;
            }
            return DocsConfig;

        }
        public static int GetDocsCount(ITracingService tracingService, IOrganizationService service, Guid ClassificationId)
        {
            var fetchXml = @"<fetch aggregate='true'>
              <entity name='cro_documentconfiguration'>
                <attribute name='cro_documentconfigurationid' alias='occurrences' aggregate='count' />
                <link-entity name='cro_cro_documentconfiguration_cro_classific' from='cro_documentconfigurationid' to='cro_documentconfigurationid' visible='false' intersect='true'>
                  <link-entity name='cro_classification' from='cro_classificationid' to='cro_classificationid' alias='ac'>
                    <filter type='and'>
                      <condition attribute='cro_classificationid' operator='eq' value='" + ClassificationId + @"' />
                    </filter>
                  </link-entity>
                </link-entity>
              </entity>
            </fetch>";
            
            var result = service.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                Entity entity = result.Entities[0];
                int totalrecords = int.Parse(((AliasedValue)entity["occurrences"]).Value.ToString());
                return totalrecords;
            }
            return 0;

        }
        public static string GetErrorMessageByCode(IOrganizationService orgService, ITracingService tracingService, string errorcode, string langagueCode)
        {
            string ErrorDesc = string.Empty;
            tracingService.Trace("res. GetErrorMessageByCode : errorcode : " + errorcode);
            string Fetch = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
                              <entity name='cro_integrationerrormessage'>
                                <attribute name='cro_name' />
                                <attribute name='cro_message' />
                                <attribute name='cro_arabicmessage' />
                                <attribute name='cro_httpstatuscode' />
                                <attribute name='cro_integrationerrormessageid' />
                                <order attribute='cro_name' descending='false' />
                                <filter type='and'>
                                  <condition attribute='statecode' operator='eq' value='0' />
                                  <condition attribute='cro_name' operator='eq' value='" + errorcode + @"' />
                                </filter>
                              </entity>
                            </fetch>";
            EntityCollection res = orgService.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res. GetErrorMessageByCode : " + res.Entities.Count);
            if (res.Entities != null && res.Entities.Count > 0)
            {
                var intgErrorMessageEntity = res.Entities[0];
                if (intgErrorMessageEntity != null)
                    ErrorDesc = langagueCode == "1033" ? Convert.ToString(intgErrorMessageEntity["cro_message"]).Trim() : Convert.ToString(intgErrorMessageEntity["cro_arabicmessage"]).Trim();
            }
            return ErrorDesc;
        }

        public static void ExecuteWorkFlow(IOrganizationService orgService, Guid workFlowId, Guid recordId)
        {

            var executeWorkflowRequest = new ExecuteWorkflowRequest()
            {
                WorkflowId = workFlowId,
                EntityId = recordId,
            };
            var executeWorkflowResponse = (ExecuteWorkflowResponse)orgService.Execute(executeWorkflowRequest);
        }

        public static OrganizationResponse ExecuteAction(IOrganizationService orgService, string actionName, string entityLogicalName, Guid recordId)
        {
            var req = new OrganizationRequest(actionName)
            {
                ["Target"] = new EntityReference(entityLogicalName, recordId)

            };
            OrganizationResponse response = orgService.Execute(req);
            return response;
        }
        public static EntityCollection CancelTasksByCase(IOrganizationService organizationService, Guid targetEntity)
        {
            EntityCollection result = null;
            var fetchXml = @"<fetch version='1.0' output-format='xml - platform' mapping='logical' distinct='false'>
<entity name ='task'>
<attribute name ='subject'/>
<attribute name ='regardingobjectid'/>
<attribute name ='activityid'/>
<attribute name ='statecode'/>
<order attribute ='subject' descending = 'false'/>
<filter type = 'and'>
<condition attribute = 'regardingobjectid' operator= 'eq' value='" +targetEntity+ @"'/>
<condition attribute='statecode' operator='eq' value='0'/>
</filter>
</entity>
</fetch> ";

            result = organizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (result != null && result.Entities != null && result.Entities.Count > 0)
            {
                return result;
            }
            return result;
        }
        public static void DeactivateDocumentsByCase(IOrganizationService organizationService, ITracingService tracingService, Guid caseId)
        {
            var fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='cro_document'>
    <attribute name='cro_documentid' />
    <attribute name='cro_name' />
    <attribute name='statecode' />
    <filter type='and'>
      <condition attribute='statecode' operator='eq' value='0' />
      <condition attribute='cro_case' operator='eq' value='" + caseId + @"' />
    </filter>
  </entity>
</fetch>";
            EntityCollection docs = organizationService.RetrieveMultiple(new FetchExpression(fetchXml));
            if (docs != null && docs.Entities != null && docs.Entities.Count > 0)
            {
                foreach (var doc in docs.Entities)
                {
                    tracingService?.Trace("Deactivating document: " + doc.Id);
                    SetStateRequest deactivate = new SetStateRequest
                    {
                        EntityMoniker = new EntityReference(Document.EntityLogicalName, doc.Id),
                        State = new OptionSetValue(1),
                        Status = new OptionSetValue(2)
                    };
                    organizationService.Execute(deactivate);
                }
            }
        }
        public static string GetKeyFrom(IOrganizationService orgService, Entity Entity, string entityLogicalName, string lookupScheme, string KeyScheme)
        {
            var refrence = (Entity.Contains(lookupScheme)) ? (EntityReference)Entity[lookupScheme] : null;
            if (refrence == null) return null;
            var entityDetails = orgService.Retrieve(entityLogicalName, refrence.Id, new ColumnSet(KeyScheme));
            var key = (entityDetails.Contains(KeyScheme)) ? entityDetails[KeyScheme].ToString() : null;
            return key;
        }

    }
    public class DuplicatedCaseResponse
    {
        public bool ActiveCaseWithSameClassification { get; set; }
        public string ExisitingCaseNumber { get; set; }
        public int status { get; set; }

    }
    #endregion

    public class JwtRequest
    {
        public string UserName { set; get; }

        public string channelId { set; get; }
    }

    public class JwtResponse
    {
        public bool success { set; get; }

        public string jwt { set; get; }
    }
    public class ActionResponse
    {
        public ActionResponse(bool IsSuccess, string ErrorMessage)
        {
            this.IsSuccess = IsSuccess;
            this.ErrorMessage = ErrorMessage;
        }
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }

    }
    public class GenericResponse
    {
        public string Message { get; set; } // here you can put you error message
        public HttpStatusCode Code { get; set; }
        public string ErrorCode { get; set; }
        public string MessageAr { get; set; }
        public static Entity GetConfigurationEntity(IOrganizationService organizationService, string entityName, string name)
        {
            ConditionExpression condition1 = new ConditionExpression();
            condition1.AttributeName = "cro_name";
            condition1.Operator = ConditionOperator.Equal;
            condition1.Values.Add(name);

            FilterExpression filter1 = new FilterExpression();
            filter1.Conditions.Add(condition1);

            QueryExpression query = new QueryExpression(entityName);
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddFilter(filter1);

            EntityCollection retrieved = organizationService.RetrieveMultiple(query);

            Entity entity = null;
            if (retrieved != null && retrieved.Entities != null && retrieved.Entities.Count > 0)
            {
                entity = retrieved.Entities[0];
            }
            return entity;
        }
        public static GenericResponse HandleIntegrationErrorMessage(IOrganizationService organizationService, string key)
        {
            GenericResponse genericResponse = new GenericResponse { Code = HttpStatusCode.InternalServerError, Message = "Something wrong happen." };
            Entity errorConfiguration = GetConfigurationEntity(organizationService, IntegrationErrorMessage.EntityLogicalName, key);
            if (errorConfiguration != null)
            {
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.HttpStatusCode) && errorConfiguration[IntegrationErrorMessage.Fields.HttpStatusCode] != null)
                {
                    int code = Convert.ToInt32(errorConfiguration[IntegrationErrorMessage.Fields.HttpStatusCode].ToString());
                    genericResponse.Code = (HttpStatusCode)code;
                }
                else
                {
                    genericResponse.Code = HttpStatusCode.InternalServerError;
                }
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.Message) && errorConfiguration[IntegrationErrorMessage.Fields.Message] != null)
                {
                    genericResponse.Message = errorConfiguration[IntegrationErrorMessage.Fields.Message].ToString();

                }
                else
                {
                    genericResponse.Message = "Something wrong happened.";
                }
                if (errorConfiguration.Contains(IntegrationErrorMessage.Fields.ArabicMessage) && errorConfiguration[IntegrationErrorMessage.Fields.ArabicMessage] != null)
                {
                    genericResponse.MessageAr = errorConfiguration["cro_arabicmessage"].ToString();
                }
                else
                {
                    genericResponse.Message = "Something wrong happened.";
                }
                if (errorConfiguration.Contains("cro_errorcode") && errorConfiguration["cro_errorcode"] != null)
                {
                    genericResponse.ErrorCode = errorConfiguration["cro_errorcode"].ToString();
                }
                else
                {
                    genericResponse.ErrorCode = "-1001";
                }

            }
            return genericResponse;
        }
    }
    public class UserSession
    {
        public string Username { get; set; }

        public Guid UserId { get; }

        public string UserBranch { get; set; }


        public string sessionLanguageEAI { get; }

        private IOrganizationService _orgService;

        public UserSession(IOrganizationService orgService, Guid userId)
        {
            _orgService = orgService;

            sessionLanguageEAI = "EN";
            UserId = userId;


            //Getting Username
            Entity userRecord = orgService.Retrieve("systemuser", userId, new ColumnSet("domainname"));
            Username = userRecord["domainname"].ToString();
            Username = (Username.Split('\\'))[1];




            //Getting Session Language
            try
            {
                Entity userSettingsRecord = orgService.Retrieve("usersettings", userId, new ColumnSet("uilanguageid"));
                if (userSettingsRecord.Contains("uilanguageid") && (int)userSettingsRecord["uilanguageid"] == 1025)
                {

                    sessionLanguageEAI = "AR";
                }
            }
            catch (Exception)
            {

            }
        }
    }



    public class OnlineIntegrationService
    {
        private string _name;
        private bool _fileLogging;
        private bool _databaseLogging;
        private Hashtable _attributes;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public bool FileLogging
        {
            get
            {
                return _fileLogging;
            }
        }

        public bool DatabaseLogging
        {
            get
            {
                return _databaseLogging;
            }
        }

        public string getAttributeValue(string name)
        {
            if (_attributes.Contains(name))
                return _attributes[name].ToString();
            else
                return null;
        }

        public OnlineIntegrationService(IOrganizationService orgService, string serviceName)
        {
            _attributes = new Hashtable();

            QueryExpression query = new QueryExpression("cro_onlineintegrationservice");
            query.Criteria = new FilterExpression();
            query.ColumnSet = new ColumnSet(true);
            query.Criteria.AddCondition("cro_name", ConditionOperator.Equal, serviceName);
            query.TopCount = 1;
            var integrationService = orgService.RetrieveMultiple(query);

            if (integrationService.Entities.Count > 0)
            {
                _name = serviceName;
                _fileLogging = false;
                _databaseLogging = false;
                if (integrationService.Entities[0].Contains("cro_filelogging"))
                    _fileLogging = bool.Parse(integrationService.Entities[0]["cro_filelogging"].ToString());
                if (integrationService.Entities[0].Contains("cro_databaselogging"))
                    _databaseLogging = bool.Parse(integrationService.Entities[0]["cro_databaselogging"].ToString());

                // get Service Attributes
                QueryExpression attributeQuery = new QueryExpression("cro_integrationattribute");
                attributeQuery.Criteria = new FilterExpression();
                attributeQuery.ColumnSet.AddColumns("cro_name", "cro_value");
                attributeQuery.Criteria.AddCondition("cro_parentid", ConditionOperator.Equal, integrationService.Entities[0].Id);


                var attributes = orgService.RetrieveMultiple(attributeQuery);
                foreach (var attribute in attributes.Entities)
                {
                    string attributeValue = "";
                    if (attribute.Contains("cro_value"))
                        attributeValue = attribute["cro_value"].ToString();

                    _attributes.Add(attribute["cro_name"].ToString(), attributeValue);
                }

            }
        }
        
    }


}
//test