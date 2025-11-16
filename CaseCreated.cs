using Croatia.CRM.Common.EntitiesEarlyBoundClasses;
using Croatia.CRM.Common.Helpers;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;

namespace Croatia.CRM.Incident.Plugin
{
    public class CaseCreated : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            string CasetypeId = null;
            string productId = null;
            string subproductId = null;
            string processId = null;
            EntityReference productER = null;
            EntityReference processER = null;
            // Obtain the tracing service
            ITracingService tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracingService.Trace("Start Execution ....");
            try
            {
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService orgService = serviceFactory.CreateOrganizationService(context.UserId);


                if (context.MessageName.ToLower() != "create" && context.Stage != 20)
                {
                    return;
                }
                if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                {

                    Entity incident = (Entity)context.InputParameters["Target"];
                    if (incident.Attributes.Contains(Case.Fields.CaseType_cro_casetype))
                    {
                        EntityReference Casetype = (EntityReference)incident.Attributes[Case.Fields.CaseType_cro_casetype];
                        CasetypeId = Casetype.Id.ToString();
                        tracingService.Trace("CasetypeId." + CasetypeId);
                    }

                    if (incident.Attributes.Contains(Case.Fields.CaseProduct))
                    {
                        productER = (EntityReference)incident.Attributes[Case.Fields.CaseProduct];
                        productId = productER.Id.ToString();
                    }

                    if (incident.Attributes.Contains(Case.Fields.CaseSubProduct))
                    {
                        EntityReference subProduct = (EntityReference)incident.Attributes[Case.Fields.CaseSubProduct];
                        subproductId = subProduct.Id.ToString();
                    }

                    if (incident.Attributes.Contains(Case.Fields.CaseProcess))
                    {
                        processER = (EntityReference)incident.Attributes[Case.Fields.CaseProcess];
                        processId = processER.Id.ToString();
                    }

                    if (!string.IsNullOrEmpty(CasetypeId) && !string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(subproductId) && !string.IsNullOrEmpty(processId))
                    {
                        Entity Classificationref = GetClassificationLevel(tracingService, orgService, CasetypeId, productId, subproductId, processId);
                        if (Classificationref != null)
                        {
                            incident[Case.Fields.CaseTitle] = productER.Name + " - " + processER.Name;
                            UpdateCaseData(Classificationref, incident, tracingService);
                            bool isPendingOnCreate = Classificationref.Contains("cro_ispendingoncreate") ? (bool)Classificationref["cro_ispendingoncreate"] : false;
                            if (isPendingOnCreate)
                            {
                                int docsCount = Helper.GetDocsCount(tracingService, orgService, Classificationref.Id);
                                if (docsCount > 0)
                                {
                                    tracingService.Trace("Docs count =" + docsCount);
                                    incident[Case.Fields.StatusReason] = new OptionSetValue((int)Case.StatusReasonEnum.Pending);
                                    //incident[Case.Fields.ConfirmPendingCaseButton] = true;
                                    incident[Case.Fields.incompleteenable] = false;
                                    incident[Case.Fields.DocumentsSubmitted] = new OptionSetValue((int)Case.DocumentsSubmittedEnum.NO);
                                }
                            }
                        }
                        else
                        {
                            throw new InvalidPluginExecutionException("Classification not found.");
                        }
                    }
                }

            }

            catch (InvalidPluginExecutionException ex)
            {

                throw new InvalidPluginExecutionException(OperationStatus.Failed, ex.Message);
            }
            catch (Exception ex)
            {

                throw new InvalidPluginExecutionException(OperationStatus.Failed, ex.Message);
            }
            finally
            {
                tracingService.Trace("End Execution.");
            }

        }
        /// <summary>
        /// UpdateCaseData
        /// </summary>
        /// <param name="classification"></param>
        /// <param name="incident"></param>
        /// <param name="orgService"></param>
        /// <param name="tracing"></param>
        private void UpdateCaseData(Entity classification, Entity incident, ITracingService tracing)
        {
            incident.Attributes[Case.Fields.ClassificationCodeLookup] = new EntityReference(Classification.EntityLogicalName, classification.Id);
            var resolveEnableFlagSchema = Case.Fields.resolveenable;
            var approveEnableFlagSchema = Case.Fields.approveenable;
            var rejectEnableFlagSchema = Case.Fields.rejectenable;
            var reopenEnableFlagSchema = "cro_reopenenable";
            var incompleteEnableFlagSchema = Case.Fields.incompleteenable;
            var PrioritySchema = Case.Fields.Priority;
            var DocsSupported = Classification.Fields.DocsSupported;
            var isMobile = Case.Fields.IsMobile;
            var mobileCase = Case.Fields.MobileCase;



            if (classification.Contains(Classification.Fields.Product) && classification.Contains(Classification.Fields.Process))
                incident[Case.Fields.CaseTitle] = ((EntityReference)classification[Classification.Fields.Product]).Name + " - " + ((EntityReference)classification[Classification.Fields.Process]).Name;
            if (classification.Contains(Classification.Fields.ClassificationCode))
                incident[Case.Fields.ClassificationCode] = classification[Classification.Fields.ClassificationCode];
            if (classification.Contains(resolveEnableFlagSchema))
                incident[resolveEnableFlagSchema] = classification[resolveEnableFlagSchema];
            if (classification.Contains(approveEnableFlagSchema))
                incident[approveEnableFlagSchema] = classification[approveEnableFlagSchema];
            if (classification.Contains(rejectEnableFlagSchema))
                incident[rejectEnableFlagSchema] = classification[rejectEnableFlagSchema];
            if (classification.Contains(reopenEnableFlagSchema))
                incident[reopenEnableFlagSchema] = classification[reopenEnableFlagSchema];
            if (classification.Contains(incompleteEnableFlagSchema))
                incident[incompleteEnableFlagSchema] = classification[incompleteEnableFlagSchema];
            if (classification.Contains(Classification.Fields.Priority))
                incident[PrioritySchema] = classification[Classification.Fields.Priority];
            if (classification.Contains(DocsSupported))
                incident[Case.Fields.SupportDocs] = classification[DocsSupported];
            if (classification.Contains(isMobile))
            {
                incident[Case.Fields.IsMobile] = classification[isMobile];
            }
            if (classification.Contains(mobileCase))
            {
                incident[mobileCase] = classification[mobileCase];
            }

            tracing.Trace($"133 --> flags = [resolve = {incident[resolveEnableFlagSchema]}" +
                $"approve = {incident[approveEnableFlagSchema]}" +
                $"reject = {incident[rejectEnableFlagSchema]}" +
                $"incomplete = {incident[incompleteEnableFlagSchema]}]");
        }

        #region query expresstion filter classification 
        //    tracingService.Trace("81");
        //    EntityReference classification = null;
        //    #region Add conditions to query
        //    FilterExpression filter = new FilterExpression(LogicalOperator.And);
        //    if (!string.IsNullOrEmpty(Casetype))
        //        filter.Conditions.Add(new ConditionExpression(Classification.Fields.CaseType, ConditionOperator.Equal, new Guid(Casetype)));

        //    if (string.IsNullOrEmpty(product))

        //        filter.Conditions.Add(new ConditionExpression(Classification.Fields.Product, ConditionOperator.Equal, new Guid(product)));
        //    //if (string.IsNullOrEmpty(subproduct))

        //    //    filter.Conditions.Add(new ConditionExpression(Classification.Fields.SubProduct, ConditionOperator.Equal, new Guid(subproduct)));

        //    if (string.IsNullOrEmpty(process))

        //    filter.Conditions.Add(new ConditionExpression(Classification.Fields.Process, ConditionOperator.Equal, new Guid(process)));
        //    if (string.IsNullOrEmpty(subprocess))

        //        filter.Conditions.Add(new ConditionExpression(Classification.Fields.SubProcess, ConditionOperator.Equal, new Guid(subprocess)));

        //    var query = new QueryExpression(Classification.EntityLogicalName)
        //    {
        //        ColumnSet = new ColumnSet(Classification.Fields.Product, Classification.Fields.SubProduct, Classification.Fields.Process, Classification.Fields.SubProcess, Classification.Fields.CaseType)

        //    };
        //    //query.Criteria.AddFilter(filter);
        //    query.Criteria = filter;
        //    #endregion

        //    EntityCollection res = service.RetrieveMultiple(query);
        //    tracingService.Trace("116" + res.Entities.Count);
        //    if (res != null && res.Entities != null && res.Entities.Count > 0)
        //           {
        //        classification = res.Entities[0].ToEntityReference();
        //        return classification;
        //    }
        //    else
        //    {
        //        return null;
        //    }   
        //      }
        #endregion
        private Entity GetClassificationLevel(ITracingService tracingService, IOrganizationService service, string Casetype, string product, string subproduct, string process)
        {
            Entity classification = null;

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
                <attribute name='cro_ispendingoncreate'/>
                <attribute name='cro_name'/>
                <attribute name='cro_subproduct'/>
                <attribute name='cro_product'/>
                <attribute name='cro_classificationcode'/>
                <attribute name='cro_docssupported'/>
                <attribute name='cro_process'/>
                <attribute name='cro_casetype'/>
                <order attribute='cro_name' descending='false'/>
                   <filter type='and'>
                  <condition attribute='cro_product' operator='eq' value='" + product + @"'/>
                  <condition attribute='cro_casetype' operator='eq' value='" + Casetype + @"'/>
                  <condition attribute='cro_process' operator='eq' value='" + process + @"'/>
                  <condition attribute='cro_subproduct' operator='eq' value='" + subproduct + @"'/>
                </filter>
              </entity>
              </fetch>";
            EntityCollection res = service.RetrieveMultiple(new FetchExpression(Fetch));
            tracingService.Trace("res." + res.Entities.Count);

            if (res != null && res.Entities != null && res.Entities.Count > 0)
            {
                classification = res.Entities[0];
                return classification;
            }
            return null;
        }


    }

}