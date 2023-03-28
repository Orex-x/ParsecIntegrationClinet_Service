using Newtonsoft.Json;
using System.IO;

namespace ParsecIntegrationClient.Services
{
    public class SettingsService
    {
        private static string fileName = "appsettings.json";

        public string _databaseConnectionString
        {
            get { return DatabaseConnectionString; }
            set { DatabaseConnectionString = value; }
        }

        public static string DatabaseConnectionString = "User = SYSDBA; " +
                "Password = temp; " +
                "Database = C:\\temp3\\RUBITECH_FOR_PARK.GDB; " +
                "DataSource = 127.0.0.1; " +
                "Port = 3050; " +
                "Dialect = 3; Charset = win1251; Role =; " +
                "Connection lifetime = 15; Pooling = true; MinPoolSize = 0; " +
                "MaxPoolSize = 50; Packet Size = 8192; ServerType = 0;";

        public string _databaseQueryString
        {
            get { return DatabaseQueryString; }
            set { DatabaseQueryString = value; }
        }

        public static string DatabaseQueryString = "select cg.id_dev, cg.id_card, cg.id_pep, p.name, " +
            "p.surname, p.patronymic, p.tabnum from" +
            " CARDINDEV_GETLIST(1) cg\r\njoin people p on p.id_pep=cg.id_pep";


        public int _databaseJobTimeout
        {
            get { return DatabaseJobTimeout; }
            set { DatabaseJobTimeout = value; }
        }

        public static int DatabaseJobTimeout = 60;


        public static void Update()
        {
            if (!File.Exists($@"{Service1.MainPath}\{fileName}"))
            {
                string json = JsonConvert.SerializeObject(new SettingsService());
                File.WriteAllText($@"{Service1.MainPath}\{fileName}", json);
            }
            else
            {
                var json = File.ReadAllText($@"{Service1.MainPath}\{fileName}");
                var settings = JsonConvert.DeserializeObject<SettingsService>(json);

                DatabaseConnectionString = settings._databaseConnectionString;
                DatabaseJobTimeout = settings._databaseJobTimeout;
                DatabaseQueryString = settings._databaseQueryString;
            }
        }
    }
}
