using ParsecIntegrationClient.Models;
using Quartz;
using System.Threading.Tasks;
using System.Web.Services.Description;

namespace ParsecIntegrationClient.Services
{
    internal class MainJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Logger.Log<MainJob>("Info", $"start MainJob");

           
            await Task.Run(() => {

                var list = DatabaseService.GetList<RowIDInDev>(SettingsService.QuerySelectIdDevCardString);

                Logger.Log<MainJob>("Info", $"count list {list.Count}");

                int i = 0;

                list.ForEach(row =>
                {
                    Logger.Log<MainJob>("Info", $"Row {i} OPERATION {row.OPERATION}");
                    switch (row.OPERATION)
                    {
                        case "1": // Добавление идентификатора человеку
                            {
                                if(row.TS_TYPE == "parsec")
                                    ParsecService.AddIdentifierPeople(row);
                                break;
                            }
                        case "2": // Удалить идентификатор
                            {
                                if (row.TS_TYPE == "parsec")
                                    ParsecService.RemoveIdentifierPeople(row);
                                break;
                            }
                        case "3": //Добавление человека
                            {
                                ParsecService.AddPeople(row);
                                break;
                            }
                        case "4": //Удаление человека
                            {
                                ParsecService.RemovePeople(row);
                                break;
                            }
                        case "5": //Добавление организации
                            {
                                ParsecService.AddOrg(row);
                                break;
                            }
                    }
                    i++;
                });
            });
        }
    }
}
