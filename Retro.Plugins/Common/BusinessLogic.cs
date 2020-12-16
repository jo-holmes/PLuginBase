﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Retro.Plugins.Common;
using System;
using System.Linq;

namespace Retro.Plugins.Common
{
  public  class BusinessLogic
    {
        public Guid CreateWorkHistory(Entity _preWorkHistory, Entity Case, IOrganizationService service)
        {
            Entity _newWorkHistory = new Entity(_preWorkHistory.LogicalName);
            EntityReference CaseOwner = FetchCaseOwner(Case);
            EntityReference CaseStatus = FetchCaseStatus(Case);
            if (CaseOwner != null)
            {
                if (CaseOwner.LogicalName.ToLower().Equals("systemuser"))
                {
                    _newWorkHistory["new_workedbyuser"] = new EntityReference(CaseOwner.LogicalName, CaseOwner.Id);
                }
                else
                {
                    _newWorkHistory["new_workedbyteam"] = new EntityReference(CaseOwner.LogicalName, CaseOwner.Id);
                }
            }
            if (CaseStatus != null)
            {
                _newWorkHistory["new_casestatusreason"] = new EntityReference(CaseStatus.LogicalName, CaseStatus.Id);
            }// _newWorkHistory["statuscode"] = new OptionSetValue(1);//1 - active

            _newWorkHistory["new_case"] = _preWorkHistory["new_case"];
            _newWorkHistory["new_name"] = _preWorkHistory["new_name"];
            Guid _newID = service.Create(_newWorkHistory);
            return _newID;
        }
        public bool UpdateWorkHistory(Entity _preWorkHistory, IOrganizationService service, Entity Case, ITracingService tracing)
        {
            tracing.Trace("inside UpdateWorkHistory method");
            bool isUpdated = false;
            //inactivate the work history record
            _preWorkHistory["new_timespendbycurrentuser"] = _preWorkHistory.Attributes.Contains("new_calctimespendbycurruser") ? _preWorkHistory.GetAttributeValue<decimal>("new_calctimespendbycurruser") : new decimal(0.00);
            _preWorkHistory["new_timespendbycaseinqueue"] = _preWorkHistory.Attributes.Contains("new_calctimespendqueue") ? _preWorkHistory.GetAttributeValue<decimal>("new_calctimespendqueue") : new decimal(0.00);
            _preWorkHistory["new_totaltimespendoncase"] = _preWorkHistory.Attributes.Contains("new_calctotaltimespendoncase") ? _preWorkHistory.GetAttributeValue<decimal>("new_calctotaltimespendoncase") : new decimal(0.00);
            if (!(_preWorkHistory.Attributes.Contains("new_workedbyuser") && _preWorkHistory["new_workedbyuser"] != null) && !(_preWorkHistory.Attributes.Contains("new_workedbyteam") && _preWorkHistory["new_workedbyteam"] != null))
            {
                DateTime _caseModifiedon = Case.GetAttributeValue<DateTime>("modifiedon");
                DateTime _workHistoryCreatedon = _preWorkHistory.GetAttributeValue<DateTime>("createdon");
                TimeSpan _timebeforeRouting = _workHistoryCreatedon.Subtract(_caseModifiedon);
                _preWorkHistory["new_timespendbeforerouting"] = Convert.ToDecimal(_timebeforeRouting.TotalHours);
            }
            EntityReference CaseStatus = FetchCaseStatus(Case);
            if (CaseStatus != null)
            {
                _preWorkHistory["dev_casestatusreason"] = new EntityReference(CaseStatus.LogicalName, CaseStatus.Id);
            }
            _preWorkHistory["statuscode"] = new OptionSetValue(293050000);//closed-293050000
            service.Update(_preWorkHistory);
            isUpdated = true;
            tracing.Trace("Updated existing  Work history:" + _preWorkHistory.Id.ToString());
            return isUpdated;
        }
        public Entity FetchPreviousWorkHistory(IOrganizationService service, Guid CaseId, ITracingService tracing)
        {
            string FetchWorkHistory = $@"<fetch >
                                          <entity name='new_queueworkhistory' >
                                            <attribute name='new_workedbyuser' />
                                            <attribute name='new_case' />
                                            <attribute name='new_name' />
                                            <attribute name='new_calctimespendbycurruser' />
                                            <attribute name='new_calctotaltimespendoncase' />
                                            <attribute name='new_casestatus' />
                                            <attribute name='new_workedbyteam' />
                                            <attribute name='new_calctimespendqueue' />
                                            <filter type='and' >
                                              <condition attribute='new_case' operator='eq' value='{CaseId}' />
                                              <condition attribute='statuscode' operator='eq' value='1' />
                                              <condition attribute='statecode' operator='eq' value='0' />
                                            </filter>
                                          </entity>
                                        </fetch>";
            Entity History = null;
            EntityCollection coll = service.RetrieveMultiple(new FetchExpression(FetchWorkHistory));

            if (coll.Entities.Count > 0)
            {
                tracing.Trace("Fetched WorkHistory Count:" + coll.Entities.Count);
                History = coll.Entities.FirstOrDefault();
            }

            return History;


        }

        public Entity RecordFetch(IOrganizationService service, string EntityLogicalName, Guid ID, string[] Columnset)
        {
            Entity RecordTobeFetched = service.Retrieve(EntityLogicalName, ID, new ColumnSet(Columnset));
            return RecordTobeFetched;
        }

        public EntityReference FetchCaseStatus(Entity Target)
        {
            EntityReference CaseStatus = null;

            if (Target.Attributes.Contains("dev_casetypestatus"))
            {
                CaseStatus = new EntityReference(Target.GetAttributeValue<EntityReference>("dev_casetypestatus").LogicalName, Target.GetAttributeValue<EntityReference>("dev_casetypestatus").Id);
            }

            return CaseStatus;

        }

        public EntityReference FetchCaseOwner(Entity Target)
        {
            EntityReference CaseOwner = null;

            if (Target.Attributes.Contains("ownerid"))
            {
                CaseOwner = new EntityReference(Target.GetAttributeValue<EntityReference>("ownerid").LogicalName, Target.GetAttributeValue<EntityReference>("ownerid").Id);
            }

            return CaseOwner;

        }
    }
}