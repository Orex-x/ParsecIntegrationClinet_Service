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



        public static void AddIdentifierPeople(DbModelRowIDInDev row)
        {
            try
            {
                var query = "select c.id_card, an.guid, p.guid as people_guid, " +
                    "p.tabnum, p.name, p.patronymic, p.surname from card c, " +
                    "accessname an join people p on p.id_pep = c.id_pep " +
                    $"where c.id_pep = {row.ID_PEP} and c.id_cardtype = 1 " +
                    $"and an.id_accessname = {row.ID_CARD}";

                var list = DatabaseService.GetList<DbModelAddIdentifier>(query);

                if(list.Count == 0)
                {
                    DatabaseService.IncrementAttemp(row);
                    Logger.Log<ParsecService>("Info", "В результате запроса к базе данных не было получено данных");
                    return;
                }
           
                foreach (var model in list)
                {
                    try
                    {
                        if (model.GUID_PEP == null || model.GUID_PEP == String.Empty)
                        {
                            Logger.Log<ParsecService>("Info", $"GUID_PEP null or empty");
                            DatabaseService.IncrementAttemp(row);
                            continue;
                        }

                        string hexValue = Convert.ToInt64(model.CODE).ToString("X8");

                        Logger.Log<ParsecService>("Info", $"Добавление группы доступа пользователю |" +
                           $"code: {row.ID_CARD} (hex: {hexValue}) | GUID_PEP = {model.GUID_PEP} " +
                           $"| tab_num = {model.TAB_NUM_PEP} " +
                           $"| ФИО (artsec): {model.SURNAME} {model.NAME} {model.PATRONYMIC}");

                        var integServ = new IntegrationService();

                        var person = integServ.GetPerson(ClientState.SessionID, new Guid(model.GUID_PEP));

                        if (person != null)
                        {
                            var res = integServ.OpenPersonEditingSession(ClientState.SessionID, person.ID);

                            if (res.Result != ClientState.Result_Success)
                            {
                                Logger.Log<ParsecService>("Error", $"Ошибка открытия сессии для редактирования пользователя. " +
                                    $"Ошибка {res.ErrorMessage}");
                                DatabaseService.IncrementAttemp(row);
                                return;
                            }

                            var _editSessionID = res.Value;

                            var accesGroup = GetAccessGroups(new Guid(model.GUID_ACCESS_GROUP));

                            Logger.Log<ParsecService>("Info", $"Получена группа доступа | NAME: {accesGroup.NAME} | GUID: {model.GUID_ACCESS_GROUP}");

                            var creatingItem = new Identifier();

                            if (!Guid.Empty.Equals(accesGroup.ID))
                                creatingItem.ACCGROUP_ID = accesGroup.ID;

                            creatingItem.IS_PRIMARY = true;
                            creatingItem.CODE = hexValue;

                            var resAddPersonIdentifier = integServ.AddPersonIdentifier(_editSessionID, creatingItem);
                            if (resAddPersonIdentifier.Result != ClientState.Result_Success)
                            {
                                Logger.Log<ParsecService>("Error", $"Ошибка при добавлении группы доступа пользователю. " +
                                    $"Ошибка: {resAddPersonIdentifier.ErrorMessage}");
                                DatabaseService.IncrementAttemp(row);
                                return;
                            }

                            Logger.Log<ParsecService>("INFO", $"Гурппа доступа успешно добавлена | " +
                               $"code: {row.ID_CARD} (hex: {hexValue}) " +
                               $"Пользователю ФИО (parsec): {person.FIRST_NAME} {person.MIDDLE_NAME} {person.LAST_NAME}");

                            DatabaseService.DeleteIdInDevById(row.ID);
                        }
                        else
                        {
                            DatabaseService.IncrementAttemp(row);
                            Logger.Log<ParsecService>("INFO", $"Пользователь с GUID: {model.GUID_PEP} не найден в parsec.");
                        }
                    }
                    catch (Exception ex)
                    {
                        DatabaseService.IncrementAttemp(row);
                        Logger.Log<ParsecService>("Exception", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void RemoveIdentifierPeople(DbModelRowIDInDev row)
        {
            var integServ = new IntegrationService();

            string hexValue = Convert.ToInt64(row.ID_CARD).ToString("X8");

            Logger.Log<ParsecService>("Info",
               $"Удаление идентификатора | {row.ID_CARD} ({hexValue})");

            var res = integServ.DeleteIdentifier(ClientState.SessionID, hexValue);
            if (res.Result != ClientState.Result_Success)
            {
                Logger.Log<ParsecService>("Error", res.ErrorMessage);
                DatabaseService.IncrementAttemp(row);
                return;
            }

            Logger.Log<ParsecService>("Info",
                $"Идентификатор успешно удален | {row.ID_CARD} ({hexValue}) ");
            DatabaseService.DeleteIdInDevById(row.ID);
        }

        public static void AddPeople(DbModelRowIDInDev row)
        {
            try
            {

                var query = "select p.id_pep, p.guid as guid_pep, " +
                    "o.guid as guid_org, p.name, p.surname, p.patronymic, p.tabnum " +
                    "from people p " +
                    "left join organization o on p.id_org=o.id_org " +
                    $"where p.id_pep={row.ID_PEP};";

                var people = DatabaseService.Get<DbModelAddPeople>(query);

                if(people != null)
                {
                    Logger.Log<ParsecService>("Info", 
                        $"Добавление пользователя " +
                        $"| ФИО: {people.SURNAME} {people.NAME} {people.PATRONYMIC} " +
                        $"TAB_NUM: {people.TABNUM}");

                    var integServ = new IntegrationService();

                    var person = new Person()
                    {
                        ID = new Guid(people.GUID_PEP),
                        FIRST_NAME = people.NAME,
                        LAST_NAME = people.SURNAME,
                        MIDDLE_NAME = people.PATRONYMIC,
                        TAB_NUM = people.TABNUM,
                        ORG_ID = new Guid(people.GUID_ORG),
                    };

                    var res = integServ.CreatePerson(ClientState.SessionID, person);

                    if (res.Result != ClientState.Result_Success)
                    {
                        Logger.Log<ParsecService>("Error", res.ErrorMessage);
                        DatabaseService.IncrementAttemp(row);
                        return;
                    }

                    Logger.Log<ParsecService>("Info", $"Пользователь добавлен успешно " +
                        $"| ФИО: {people.SURNAME} {people.NAME} {people.PATRONYMIC} " +
                        $"TAB_NUM: {people.TABNUM} GUID: {people.GUID_PEP}");

                    DatabaseService.DeleteIdInDevById(row.ID);
                    return;
                }

                Logger.Log<ParsecService>("Info", $"Пользователь с {row.ID_PEP} не найден");
                DatabaseService.IncrementAttemp(row);
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void RemovePeople(DbModelRowIDInDev row)
        {
            try
            {
                var integServ = new IntegrationService();

                //В этом случае в столбце ID_CARD храниться GUID пипла
                var person = integServ.GetPerson(ClientState.SessionID, new Guid(row.ID_CARD));

                var res = integServ.DeletePerson(ClientState.SessionID, person.ID);

                if (res.Result != ClientState.Result_Success)
                {
                    Logger.Log<ParsecService>("Error", $"Ошибка при удалении пользователя. " +
                        $"Ошибка: {res.ErrorMessage}");
                    DatabaseService.IncrementAttemp(row);
                    return;
                }


                RefreshOrgUnitsHierarhy();

                Logger.Log<ParsecService>("Info",
                 $"пользователь успешно удален |" +
                 $"{person.FIRST_NAME} {person.MIDDLE_NAME} {person.LAST_NAME}");
                DatabaseService.DeleteIdInDevById(row.ID);
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void AddOrg(DbModelRowIDInDev row)
        {
            try
            {

                var query = "select o.guid as guide_for_add, " +
                    "o2.guid as guid_for_parent, o.name, o.divcode, o.id_org from organization o " +
                    "join organization o2 on o2.id_org=o.id_parent " +
                    $"where o.guid='{row.ID_CARD}'";

                var model = DatabaseService.Get<DbModelAddOrg>(query);



                Logger.Log<ParsecService>("Info", $"Добавление организации |" +
                    $" NAME: {model.NAME} DIVCODE: {model.DIVCODE}");


       

                if (model.NAME == null)
                {
                    Logger.Log<ParsecService>("Error", $"Органиация {row.ID_CARD} не найдена");
                    DatabaseService.IncrementAttemp(row);
                    return;
                }

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

                Logger.Log<ParsecService>("Info", $"Организация добавлена успешно: {org.NAME} " +
                    $"| ID: {org.ID} Parent ID: {org.PARENT_ID} " +
                    $"divcode: {model.DIVCODE} IdOrg: {model.ID_ORG}");

                DatabaseService.DeleteIdInDevById(row.ID);
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void RemoveOrg(DbModelRowIDInDev row)
        {
            try
            {
                var integServ = new IntegrationService();

                Logger.Log<ParsecService>("Info",
                   $"Удаление организации | {row.ID_CARD}");

                var res = integServ.DeleteOrgUnit(ClientState.SessionID, new Guid(row.ID_CARD));
                if (res.Result != ClientState.Result_Success)
                {
                    Logger.Log<ParsecService>("Error", res.ErrorMessage);
                    DatabaseService.IncrementAttemp(row);
                    return;
                }

                Logger.Log<ParsecService>("Info",
                    $"Организация успешно удалена | {row.ID_CARD}");
                DatabaseService.DeleteIdInDevById(row.ID);

            }
            catch(Exception ex)
            {

            }
        }

        public static void AddCardPeople(DbModelRowIDInDev row)
        {
            try
            {
                var query = "select p.guid, p.tabnum, p.name, p.patronymic, " +
                    $"p.surname from people p where p.id_pep = {row.ID_PEP}";


                var list = DatabaseService.GetList<DbModelAddCard>(query);

                foreach (var model in list)
                {
                    try
                    {
                        if (model.GUID_PEP == null || model.GUID_PEP == String.Empty)
                        {
                            Logger.Log<ParsecService>("Info", $"GUID_PEP null or empty");
                            DatabaseService.IncrementAttemp(row);
                            continue;
                        }
                        string hexValue = Convert.ToInt64(row.ID_CARD).ToString("X8");

                        Logger.Log<ParsecService>("Info", $"Добавление карты пользователю |" +
                            $"code: {row.ID_CARD} (hex: {hexValue}) | GUID_PEP = {model.GUID_PEP} " +
                            $"| tab_num = {model.TAB_NUM_PEP} " +
                            $"| ФИО (artsec): {model.SURNAME} {model.NAME} {model.PATRONYMIC}");

                        var integServ = new IntegrationService();

                        var person = integServ.GetPerson(ClientState.SessionID, new Guid(model.GUID_PEP));

                        if (person != null)
                        {
                            var res = integServ.OpenPersonEditingSession(ClientState.SessionID, person.ID);

                            if (res.Result != ClientState.Result_Success)
                            {
                                Logger.Log<ParsecService>("Error", $"Ошибка открытия сессии для редактирования пользователя. " +
                                    $"Ошибка {res.ErrorMessage}");
                                DatabaseService.IncrementAttemp(row);
                                return;
                            }

                            var _editSessionID = res.Value;

                            var creatingItem = new BaseIdentifier();

                            creatingItem.IS_PRIMARY = true;
                            creatingItem.CODE = hexValue;
                            creatingItem.PERSON_ID = new Guid(model.GUID_PEP);

                            var resAddPersonIdentifier = integServ.AddPersonIdentifier(_editSessionID, creatingItem);
                            if (resAddPersonIdentifier.Result != ClientState.Result_Success)
                            {
                                Logger.Log<ParsecService>("Error", $"Ошибка при добавлении карты пользователю." +
                                    $"Ошибка: {resAddPersonIdentifier.ErrorMessage}");
                                DatabaseService.IncrementAttemp(row);
                                return;
                            }

                            Logger.Log<ParsecService>("INFO", $"Карта успешно добавлена | " +
                                $"code: {row.ID_CARD} (hex: {hexValue}) " +
                                $"Пользователю ФИО (parsec): {person.FIRST_NAME} {person.MIDDLE_NAME} {person.LAST_NAME}");

                            DatabaseService.DeleteIdInDevById(row.ID);
                        }
                        else
                        {
                            DatabaseService.IncrementAttemp(row);
                            Logger.Log<ParsecService>("INFO", $"Пользователь с GUID: {model.GUID_PEP} не найден в parsec.");
                        }
                    }
                    catch (Exception ex)
                    {
                        DatabaseService.IncrementAttemp(row);
                        Logger.Log<ParsecService>("Exception", ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                DatabaseService.IncrementAttemp(row);
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
        }

        public static void RemoveCardPeople(DbModelRowIDInDev row)
        {
            var integServ = new IntegrationService();

            string hexValue = Convert.ToInt64(row.ID_CARD).ToString("X8");

            Logger.Log<ParsecService>("Info",
               $"Удаление карты | {row.ID_CARD} (hex: {hexValue})");

            var res = integServ.DeleteIdentifier(ClientState.SessionID, hexValue);
            if (res.Result != ClientState.Result_Success)
            {
                Logger.Log<ParsecService>("Error", res.ErrorMessage);
                DatabaseService.IncrementAttemp(row);
                return;
            }

            Logger.Log<ParsecService>("Info",
                $"Карта успешно удалена | {row.ID_CARD} (hex: {hexValue}) ");
            DatabaseService.DeleteIdInDevById(row.ID);
        }
    }
}
