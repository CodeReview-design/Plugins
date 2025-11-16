using Croatia.CRM.Common.EntitiesEarlyBoundClasses;
using Croatia.CRM.Common.Helpers;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Croatia.CRM.Incident.Plugin
{
    public class ChangeCaseClassification : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            string CasetypeId = null;
            string productId = null;
            string subproductId = null;
            string processId = null;
            string PreImgCasetypeId = null;
            string PreImgProductId = null;
            string PreImgSubproductId = null;
            string PreImgProcessId = null;
            string PreImgSamaRefnumber = null;
            Guid customerGuid = Guid.Empty;
            EntityReference productER = null;
            EntityReference processER = null;
            bool IsClassifcationChanged = true;
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
                    var casePreImg = context.PreEntityImages["Img"];
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
                        tracingService.Trace("productId." + productId);
                    }

                    if (incident.Attributes.Contains(Case.Fields.CaseSubProduct))
                    {
                        EntityReference subProduct = (EntityReference)incident.Attributes[Case.Fields.CaseSubProduct];
                        subproductId = subProduct.Id.ToString();
                        tracingService.Trace("subproductId." + subproductId);
                    }

                    if (incident.Attributes.Contains(Case.Fields.CaseProcess))
                    {
                        processER = (EntityReference)incident.Attributes[Case.Fields.CaseProcess];
                        processId = processER.Id.ToString();
                        tracingService.Trace("processId." + processId);
                    }

                    if (incident.Attributes.Contains(Case.Fields.Customer))
                        customerGuid = ((EntityReference)incident.Attributes[Case.Fields.Customer]).Id;

                    if (casePreImg.Contains(Case.Fields.CaseType_cro_casetype))
                    {
                        PreImgCasetypeId = Convert.ToString(((EntityReference)casePreImg.Attributes[Case.Fields.CaseType_cro_casetype]).Id);
                        tracingService.Trace("PreImgCasetypeId ID: " + PreImgCasetypeId);
                        CasetypeId = CasetypeId == null ? PreImgCasetypeId : CasetypeId;
                    }
                    if (casePreImg.Contains(Case.Fields.CaseProduct))
                    {
                        PreImgProductId = Convert.ToString(((EntityReference)casePreImg.Attributes[Case.Fields.CaseProduct]).Id);
                        tracingService.Trace("PreImgProductId ID: " + PreImgProductId);
                    }
                    if (casePreImg.Contains(Case.Fields.CaseSubProduct))
                    {
                        PreImgSubproductId = Convert.ToString(((EntityReference)casePreImg.Attributes[Case.Fields.CaseSubProduct]).Id);
                        tracingService.Trace("PreImgSubproductId ID: " + PreImgSubproductId);
                    }
                    if (casePreImg.Contains(Case.Fields.CaseProcess))
                    {
                        PreImgProcessId = Convert.ToString(((EntityReference)casePreImg.Attributes[Case.Fields.CaseProcess]).Id);
                        tracingService.Trace("PreImgProcessId ID: " + PreImgProcessId);
                    }

                    if (casePreImg.Contains("cro_samareferencenumber"))
                    {
                        PreImgSamaRefnumber = Convert.ToString(casePreImg.Attributes["cro_samareferencenumber"]);
                        tracingService.Trace("PreImgSamaRefnumber ID: " + PreImgSamaRefnumber);

                    }

                    if (!string.IsNullOrEmpty(CasetypeId) && !string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(subproductId) && !string.IsNullOrEmpty(processId))
                    {
                        if (PreImgCasetypeId == CasetypeId && PreImgProductId == productId && PreImgProcessId == processId
                        && PreImgSubproductId == subproductId)
                            IsClassifcationChanged = false;
                    }
                    tracingService.Trace("IsClassifcationChanged: " + IsClassifcationChanged);

                    if (IsClassifcationChanged)
                    {
                        Entity Classificationref = GetClassificationLevel(tracingService, orgService, CasetypeId, productId, subproductId, processId);
                        if (Classificationref != null)
                        {
                            #region check Duplicat Case
                            var response = Helper.activeCaseWithSameClassification(tracingService, orgService, incident, customerGuid, Guid.Parse(CasetypeId), Guid.Parse(productId), Guid.Parse(subproductId), Guid.Parse(processId));
                            tracingService.Trace("ActiveCaseWithSameClassification: " + response.ActiveCaseWithSameClassification);

                            if (response.ActiveCaseWithSameClassification)
                            {
                                tracingService.Trace("ExisitingCaseNumber: " + response.ExisitingCaseNumber);
                                String duplicationMessage = "Customer already has open case with same classification.\n Exisiting Case Number is " + response.ExisitingCaseNumber;
                                throw new InvalidPluginExecutionException(OperationStatus.Failed, 20, duplicationMessage);
                            }
                            #endregion

                            incident[Case.Fields.CaseTitle] = productER.Name + " - " + processER.Name;
                            if (PreImgSamaRefnumber != null)
                                incident[Case.Fields.SAMAReferenceNo] = PreImgSamaRefnumber;
                            UpdateCaseData(Classificationref, incident, tracingService);
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

        private Entity GetClassificationLevel(ITracingService tracingService, IOrganizationService service, string Casetype, string product, string subproduct, string process)
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
                return res.Entities[0];
            }
            return null;
        }

    }
}