using ParsecIntegrationClient.IntegrationWebService;
using Quartz;
using System.Threading.Tasks;

namespace ParsecIntegrationClient.Services
{
    internal class DatabaseJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await Task.Run(() => {
                var list = DatabaseService.GetPeoples();
                Logger.Log<DatabaseJob>("Info", $"count peoples in list {list.Count}");

                list.ForEach(p => {
                    var person = new Person()
                    {
                        FIRST_NAME = p.NAME,
                        LAST_NAME = p.SURNAME,
                        MIDDLE_NAME = p.PATRONYMIC,
                        TAB_NUM = p.TABNUM,
                    };

                    var result = ParsecService.CreatePerson(person);
                    Logger.Log<DatabaseJob>("Info", $"Create person successful| ID: {result.ID}");
                });
            });
        }
    }
}
