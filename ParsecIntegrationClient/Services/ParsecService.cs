using ParsecIntegrationClient.IntegrationWebService;
using ParsecIntegrationClient.Models;
using System;

namespace ParsecIntegrationClient.Services
{
    public class ParsecService
    {
        public static Person CreatePerson(Person person)
        {
            try
            {
                var integServ = new IntegrationService();

                var res = integServ.CreatePerson(ClientState.SessionID, person);
                if (res.Result != ClientState.Result_Success)
                {
                    Logger.Log<ParsecService>("Error", res.ErrorMessage);
                    Console.WriteLine(res.ErrorMessage);
                    return null;
                }
                person.ID = res.Value;
                return person;
            }
            catch (Exception ex)
            {
                Logger.Log<ParsecService>("Exception", ex.Message);
            }
            return null;
        }
    }
}
