using FirebirdSql.Data.FirebirdClient;
using ParsecIntegrationClient.Models;
using System;
using System.Collections.Generic;

namespace ParsecIntegrationClient.Services
{
    public class DatabaseService
    {
        public static List<People> GetPeoples()
        {
            var connectionString = SettingsService.DatabaseConnectionString;

            using (var connection = new FbConnection(connectionString))
            {
                try
                {
                    Logger.Log<DatabaseService>("Info", "Connecting to database...");
                    connection.Open();
                    Logger.Log<DatabaseService>("Info", "Connected to database...");

                    var cmd = new FbCommand(SettingsService.DatabaseQueryString, connection);

                    var peoples = new List<People>();

                    using (FbDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            Logger.Log<DatabaseService>("Info", "FbDataReader Read");
                            var people = new People();
                            people.ID_DEV = dr.GetString(0);
                            people.ID_CARD = dr.GetString(1);
                            people.ID_PEP = dr.GetString(2);
                            people.NAME = dr.GetString(3);
                            people.SURNAME = dr.GetString(4);
                            people.PATRONYMIC = dr.GetString(5);
                            people.TABNUM = dr.GetString(6);
                            peoples.Add(people);
                            Logger.Log<DatabaseService>("Info", $"People : {people.ID_PEP} | {people.NAME}");
                        }
                    }

                    return peoples;
                }
                catch (Exception ex)
                {
                    Logger.Log<DatabaseService>("Exception", $"{ex.Message}");
                }
                return new List<People>();
            }
        }
    }
}
