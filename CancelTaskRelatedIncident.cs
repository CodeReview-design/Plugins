using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;
using System;
using System.Activities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Croatia.CRM.Common.Helpers;
namespace Croatia.CRM.Incident.CustomStep1
{
    public class CancelTaskRelatedIncident : CodeActivity
    {

        protected override void Execute(CodeActivityContext executionContext)
        {
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();
            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            Guid incidentId = context.PrimaryEntityId;
            #region CancelTasksByCase
            EntityCollection tasks = Helper.CancelTasksByCase(service, incidentId);

            if (tasks != null && tasks.Entities.Count > 0)
            {

                foreach (var task in tasks.Entities)
                {
                    Entity updateTask = new Entity("task", task.Id)
                    {
                        ["statecode"] = new OptionSetValue(2)
                    };

                    service.Update(updateTask);
                }
            }
            #endregion

        }
    }
}
    