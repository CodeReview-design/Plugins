using Croatia.CRM.Common.EntitiesEarlyBoundClasses;
using Croatia.CRM.Common.Helpers;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.ServiceModel;

namespace Croatia.CRM.Incident.Plugin
{
    public class MapCaseDocuments : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
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
                    if (incident.Contains(Case.Fields.ClassificationCodeLookup) && incident.Contains(Case.Fields.SupportDocs))
                    {
                        Guid ClassificationId = ((EntityReference)incident[Case.Fields.ClassificationCodeLookup]).Id;


                        if (ClassificationId != Guid.Empty && (bool)incident[Case.Fields.SupportDocs])
                        {
                            MapCaseDcocuemts(ClassificationId, incident, tracingService, orgService);
                        }
                    }
                    //run assign case WF
                    Entity case1 = new Entity(Case.EntityLogicalName, incident.Id);
                    case1[Case.Fields.IsAssign] = true;
                    orgService.Update(case1);
                    tracingService.Trace("IsAssign =" + true);

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

        private void MapCaseDcocuemts(Guid classificationId, Entity incident, ITracingService tracingService, IOrganizationService service)
        {
            DataCollection<Entity> DocsConfig = Helper.GetDocsConfigByClassificationID(tracingService, service, classificationId);
            if (DocsConfig != null && DocsConfig.Count > 0)
            {
                // for loop 
                foreach (Entity entity in DocsConfig)
                {
                    Entity NewDocument = new Entity(Document.EntityLogicalName);

                    if (entity.Contains(DocumentConfiguration.Fields.Name))
                    {
                        // create record
                        NewDocument[Document.Fields.Title] = entity[DocumentConfiguration.Fields.Name];
                        NewDocument[Document.Fields.Case] = new EntityReference(Case.EntityLogicalName, incident.Id);
                        if (entity.Contains(DocumentConfiguration.Fields.ArabicName))
                        {
                            NewDocument[Document.Fields.ArabicTitle] = entity[DocumentConfiguration.Fields.ArabicName];
                        }
                        if (entity.Contains(DocumentConfiguration.Fields.Description))
                        {
                            NewDocument[Document.Fields.Description] = entity[DocumentConfiguration.Fields.Description];
                        }
                        if (entity.Contains(DocumentConfiguration.Fields.ArabicDescription))
                        {
                            NewDocument[Document.Fields.ArabicDescription] = entity[DocumentConfiguration.Fields.ArabicDescription];
                        }
                        if (entity.Contains(DocumentConfiguration.Fields.IsRequired))
                        {
                            NewDocument[Document.Fields.IsRequired] = entity[DocumentConfiguration.Fields.IsRequired];
                        }

                    }
                    service.Create(NewDocument);
                }
            }
            else
                tracingService.Trace("No Documents in Classification Document Configuration");



        }






    }

}