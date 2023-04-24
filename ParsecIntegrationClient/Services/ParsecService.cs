using ParsecIntegrationClient.IntegrationWebService;
using ParsecIntegrationClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParsecIntegrationClient.Services
{
    public class ParsecService
    {


        public static AccessGroup GetAccessGroups(Guid idGroup)
        {
            var integServ = new IntegrationService();
            var accesGroups = integServ.GetAccessGroups(ClientState.SessionID).ToList();
            foreach(var item in accesGroups)
                if(item.ID.Equals(idGroup))
                    return item;

            return null;
        }

        public static void RefreshOrgUnitsHierarhy()
        {
            try
            {
                var integServ = new IntegrationService();
                var ouHierarhy = integServ.GetOrgUnitsHierarhy(ClientState.SessionID);
                ClientState.SetOrgUnitHierarhy(ouHierarhy);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        

        public static void AddIdentifierPeople(RowIDInDev row)
        {
            try
            {
                var query = "select cd.id_card as code, p.guid as guid_pep, an.guid as guid_access_group from cardindev cd " +
                    "left join device d on d.id_dev=cd.id_dev " +
                    "left join device d2 on d2.id_ctrl=d.id_ctrl and d2.id_reader is null " +
                    "left join people p on p.id_pep=cd.id_pep " +
                    "left join servertypelist stl on d2.id_server=stl.id_server " +
                    "left join access a on a.id_dev=cd.id_dev " +
                    "left join accessname an on an.id_accessname=a.id_accessname " +
                    "left join servertype st on st.id=stl.id_type and st.sname='parsec' " +
                    $"where cd.id_pep={row.ID_PEP} and cd.id_card='{row.ID_CARD}' and st.sname='parsec';";
                
                
                var list = DatabaseService.GetList<DbModelAddIdentifier>(query);

                Logger.Log<ParsecService>("Info", $"list of DbModelAddIdentifier count: {list.Count}");

                var blackList = new List<string>();
                
                foreach (var model in list)
                {
                    try
                    {
                        if (model.GUID_PEP == null || model.GUID_PEP == String.Empty)
                        {
                            Logger.Log<ParsecService>("Info", $"model.GUID_PEP null or empty {model.GUID_PEP}");
                            DatabaseService.IncrementAttemp(row);
                            continue;
                        }

                        if (blackList.Contains(model.GUID_PEP)) {
                            continue;
                        }
                        

                        string hexValue = Convert.ToInt64(model.CODE).ToString("X8");

                        Logger.Log<ParsecService>("Info", $"AddIdentifierPeople: " +
                            $"GUID_PEP = {model.GUID_PEP} | hexValue: {hexValue}");

                        var integServ = new IntegrationService();

                        var person = integServ.GetPerson(ClientState.SessionID, new Guid(model.GUID_PEP));

                        if (person != null)
                        {
                            var res = integServ.OpenPersonEditingSession(ClientState.SessionID, person.ID);

                            if (res.Result != ClientState.Result_Success)
                            {
                                Logger.Log<ParsecService>("Error", "OpenPersonEditingSession " + res.ErrorMessage);
                                DatabaseService.IncrementAttemp(row);
                                return;
                            }
                            Logger.Log<ParsecService>("Info", $"Open session");

                            var _editSessionID = res.Value;

                            var accesGroup = GetAccessGroups(new Guid(model.GUID_ACCESS_GROUP));

                            var creatingItem = new Identifier();

                            if (!Guid.Empty.Equals(accesGroup.ID))
                                creatingItem.ACCGROUP_ID = accesGroup.ID;

                            creatingItem.IS_PRIMARY = true;
                            creatingItem.CODE = hexValue;

                            var resAddPersonIdentifier = integServ.AddPersonIdentifier(_editSessionID, creatingItem);
                            if (resAddPersonIdentifier.Result != ClientState.Result_Success)
                            {
                                Logger.Log<ParsecService>("Error", "AddPersonIdentifier: " + resAddPersonIdentifier.ErrorMessage);
                                DatabaseService.IncrementAttemp(row);
                                return;
                            }

                            Logger.Log<ParsecService>("INFO", $"Успешное добавление карточки {model.CODE} ({hexValue}) " +
                                $"человеку {person.FIRST_NAME} {person.MIDDLE_NAME} {person.FIRST_NAME}");

                            DatabaseService.DeleteIdInDevById(row.ID);
                        }
                        else
                        {
                            blackList.Add(model.GUID_PEP);
                            DatabaseService.IncrementAttemp(row);
                            Logger.Log<ParsecService>("INFO", $"Человек с GUID: {model.GUID_PEP} не найден в parsec.");
                        }
                    }
                    catch(Exception ex)
                    {
                        DatabaseService.IncrementAttemp(row);
                        Logger.Log<ParsecService>("Exception", ex.Message);
                    }
                }
            }
            catch(Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void AddPeople(RowIDInDev row)
        {
            try
            {

                Logger.Log<ParsecService>("Info", $"Добавление человека {row.ID_PEP}");


                var query = "select p.id_pep, p.guid as guid_pep, o.guid as guid_org, p.name, p.surname, p.patronymic, p.tabnum " +
                    "from people p " +
                    "left join organization o on p.id_org=o.id_org " +
                    $"where p.id_pep={row.ID_PEP};";

                var people = DatabaseService.Get<DbModelAddPeople>(query);

                if(people != null)
                {
                    var integServ = new IntegrationService();

                    var person = new Person()
                    {
                        ID = new Guid(people.GUID_PEP),
                        FIRST_NAME = people.NAME,
                        LAST_NAME = people.SURNAME,
                        MIDDLE_NAME = people.ID_PATRONYMIC,
                        TAB_NUM = people.TABNUM,
                        ORG_ID = new Guid(people.GUID_ORG),
                    };

                    Logger.Log<ParsecService>("Info", $"CreatePerson {person.ID} {person.LAST_NAME}");

                    var res = integServ.CreatePerson(ClientState.SessionID, person);

                    if (res.Result != ClientState.Result_Success)
                    {
                        Logger.Log<ParsecService>("Error", res.ErrorMessage);
                        DatabaseService.IncrementAttemp(row);
                        return;
                    }

                    Logger.Log<ParsecService>("Info", $"Create person successful| ID: {person.ID}");
                    DatabaseService.DeleteIdInDevById(row.ID);
                    return;
                }

                DatabaseService.IncrementAttemp(row);
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }


        public static void AddOrg(RowIDInDev row)
        {
            try
            {
                var query = "select o.guid as guide_for_add, " +
                    "o2.guid as guid_for_parent, o.name from organization o " +
                    "join organization o2 on o2.id_org=o.id_parent " +
                    $"where o.guid='{row.ID_CARD}'";

                var model = DatabaseService.Get<DbModelAddOrg>(query);

                var integServ = new IntegrationService();

                var org = new OrgUnit()
                {
                    NAME = model.NAME,
                    ID = new Guid(model.GUID),
                    PARENT_ID = new Guid(model.GUID_PARENT),
                    DESC = ""
                };

                var result = integServ.CreateOrgUnit(ClientState.SessionID, org);
                if (result.Result != ClientState.Result_Success)
                {
                    Logger.Log<ParsecService>("Error", result.ErrorMessage);
                    DatabaseService.IncrementAttemp(row);
                    return;
                }

                Logger.Log<ParsecService>("INFO", $"Create org: {org.NAME} with id {org.ID} and parent id {org.PARENT_ID}");

                DatabaseService.DeleteIdInDevById(row.ID);
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void RemovePeople(RowIDInDev row)
        {
            try
            {
                var integServ = new IntegrationService();

                //В этом случае в столбце ID_CARD храниться GUID пипла
                var person = integServ.GetPerson(ClientState.SessionID, new Guid(row.ID_CARD));

                var res = integServ.DeletePerson(ClientState.SessionID, person.ID);

                if (res.Result != ClientState.Result_Success)
                {
                    Logger.Log<ParsecService>("Error", res.ErrorMessage);
                    DatabaseService.IncrementAttemp(row);
                    return;
                }


                RefreshOrgUnitsHierarhy();

                Logger.Log<ParsecService>("Info",
                 $"Удаление человека " +
                 $"{person.FIRST_NAME} {person.MIDDLE_NAME} {person.FIRST_NAME} " +
                 $"прошло успешно");
                DatabaseService.DeleteIdInDevById(row.ID);
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void RemoveIdentifierPeople(RowIDInDev row)
        {
            var integServ = new IntegrationService();

            string hexValue = Convert.ToInt64(row.ID_CARD).ToString("X8");

            Logger.Log<ParsecService>("Info",
               $"Удаление идентификатора {row.ID_CARD} ({hexValue})");

            var res = integServ.DeleteIdentifier(ClientState.SessionID, hexValue);
            if (res.Result != ClientState.Result_Success)
            {
                Logger.Log<ParsecService>("Error", res.ErrorMessage);
                DatabaseService.IncrementAttemp(row);
                return;
            }

            Logger.Log<ParsecService>("Info",
                $"Удаление идентификатора {row.ID_CARD} ({hexValue}) прошло успешно");
            DatabaseService.DeleteIdInDevById(row.ID);
        }
    }
}
