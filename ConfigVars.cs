using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FXPostgres
{
    public sealed class ConfigVars
    {
        public string clientID = string.Empty;
        public string KafkaTopic = string.Empty;
        public string EnpointUrl = string.Empty;
        public string PostgresDBConnectionString = string.Empty;

        private ConfigVars()
        {
            clientID = Environment.GetEnvironmentVariable("ClientID");
            EnpointUrl = Environment.GetEnvironmentVariable("KAFKA_REST_API_URL");
            KafkaTopic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            //for local testing
            //clientID = "29481765";
            //KafkaTopic = "FX-VIP";
            //EnpointUrl = "http://fx-kafka.herokuapp.com/api/messages";
            //databaseUrl = "postgres://uf8kp0cv89272b:pab1js89eefi04ajbjs208mb9ld@ec2-52-55-23-133.compute-1.amazonaws.com:5432/da1lhtac3qjuqs";
            if (!string.IsNullOrEmpty(databaseUrl))
            {
                string conStr = databaseUrl.Replace("//", "");
                char[] delimiterChars = { '/', ':', '@', '?' };
                string[] strConn = conStr.Split(delimiterChars);
                strConn = strConn.Where(x => !string.IsNullOrEmpty(x)).ToArray();
                PostgresDBConnectionString = string.Format("Host={0};Port={1};Database={2};User ID={3};Password={4};sslmode=Require;Trust Server Certificate=true;", strConn[3], strConn[4], strConn[5], strConn[1], strConn[2]);
            }
        }
        public static ConfigVars Instance { get { return ConfigVarInstance.Instance; } }

        private class ConfigVarInstance
        {
            static ConfigVarInstance()
            {
            }

            internal static readonly ConfigVars Instance = new ConfigVars();
        }
    }
}
