using Croatia.CRM.Common.EntitiesEarlyBoundClasses;
using Croatia.CRM.Common.Helpers;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;

namespace Croatia.CRM.Incident.Plugin
{
    public class AssignCase : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            tracing.Trace("Start Execution ....");
            try
            {
                IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService organizationService = serviceFactory.CreateOrganizationService(context.UserId);
                Guid incidentId = context.PrimaryEntityId;
                Entity incidentDetails = organizationService.Retrieve(Case.EntityLogicalName,
                        incidentId, new ColumnSet(Case.Fields.IsAssign, Case.Fields.Status, Case.Fields.ClassificationCodeLookup, Case.Fields.AssignedQueue));
                if (!incidentDetails.Contains(Case.Fields.ClassificationCodeLookup) ||
                    !incidentDetails.Contains(Case.Fields.IsAssign) ||
                    !(bool)incidentDetails[Case.Fields.IsAssign])
                    return;
                EntityReference currentUser = new EntityReference(User.EntityLogicalName, context.InitiatingUserId);
                tracing.Trace($"[1] --> classification = value , is assign = true, currentUser = {currentUser.Name}");
                Guid classificationId = ((EntityReference)incidentDetails.Attributes[Case.Fields.ClassificationCodeLookup]).Id;
                Entity classification = organizationService.Retrieve(Classification.EntityLogicalName, classificationId, new ColumnSet(Classification.Fields.User, Classification.Fields.Team, Classification.Fields.Queue, Classification.Fields.AssignTo, Classification.Fields.Name));
                tracing.Trace($"[2] --> classification = {classification[Classification.Fields.Name]}");
                // Check for an existing queue item for this case
                QueryExpression queueItemQuery = new QueryExpression("queueitem")
                {
                    ColumnSet = new ColumnSet("queueitemid"),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression("objectid", ConditionOperator.Equal, incidentId) }
                    }
                };
                EntityCollection queueItems = organizationService.RetrieveMultiple(queueItemQuery);

                // Deleting a queue item if it's found, 
                if (queueItems.Entities.Count > 0)
                {
                    foreach (var queueItem in queueItems.Entities)
                    {
                        organizationService.Delete("queueitem", queueItem.Id);
                        tracing.Trace($"Deleted queue item for case: {incidentId}");
                    }
                }
                Helper.Assign(classification, Classification.EntityLogicalName, incidentDetails, incidentDetails.Id, currentUser, organizationService, tracing);
                //#region update BPF
                ////move to review case stage
                tracing.Trace("incidentDetails.Id --> " + incidentDetails.Id);
                Helper.UpdateBPF(incidentDetails.Id, Helper.ProcessStage.CaseReviewGuid, 0, 1, organizationService, tracing);
                
                tracing.Trace("end code activity");

            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                throw new InvalidPluginExecutionException(OperationStatus.Failed, ex.Message);
            }
        }
    }
}
