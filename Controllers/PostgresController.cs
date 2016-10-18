using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using FXPostgres.Models;
using Npgsql;
using System.Text;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace FXPostgres.Controllers
{
    public class PostgresController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            ViewBag.ClientID = ConfigVars.Instance.clientID;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> ReadKafkaMessage(string eventSource, bool isReadFromFirst)
        {
            try
            {
                int lastOffset = -1;
                if (!isReadFromFirst)
                    lastOffset = getLastKafkaOffset();

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Accept.Clear();
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(string.Format("{0}", ConfigVars.Instance.EnpointUrl))
                    };
                    var response = await httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var kafkaResponse = await response.Content.ReadAsStringAsync();
                        var kafkaMessage = JsonConvert.DeserializeObject<List<KafkaMessage>>(kafkaResponse);
                        if (kafkaMessage != null)
                        {
                            if (isReadFromFirst && kafkaMessage.Where(p => p.offset > 0 && p.topic.ToUpper() == ConfigVars.Instance.KafkaTopic.ToUpper()).Count() > 0)
                                return Json(kafkaMessage.Where(p => p.offset > 0 && p.topic.ToUpper() == ConfigVars.Instance.KafkaTopic.ToUpper()).ToList());
                            else if (kafkaMessage.Where(p => p.offset > lastOffset && p.topic.ToUpper() == ConfigVars.Instance.KafkaTopic.ToUpper()).Count() > 0)
                                return Json(kafkaMessage.Where(p => p.offset > lastOffset && p.topic.ToUpper() == ConfigVars.Instance.KafkaTopic.ToUpper()).ToList());
                        }
                    }

                    return Json(null);
                }
            }
            catch (Exception exception)
            {
                return Json(null);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> WriteKafkaMessage(List<KafkaMessage> kafkaMessage, bool isWriteFromFirst)
        {
            try
            {
                int lastOffset = -1;
                if (!isWriteFromFirst)
                {
                    lastOffset = getLastKafkaOffset();
                    kafkaMessage = kafkaMessage.Where(p => p.offset > lastOffset).ToList();
                }
                using (var sqlCon = new NpgsqlConnection(ConfigVars.Instance.PostgresDBConnectionString))
                {
                    sqlCon.Open();
                    if (isWriteFromFirst)
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand("delete from fxvip;", sqlCon))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                for (int i = 0; i < kafkaMessage.Count; i++)
                {
                    using (var sqlCon = new NpgsqlConnection(ConfigVars.Instance.PostgresDBConnectionString))
                    {
                        sqlCon.Open();
                        using (NpgsqlCommand cmd = new NpgsqlCommand(string.Format("insert into fxvip(\"Offset\",\"Message\",\"Topic\",\"Partition\")values('{0}','{1}','{2}','{3}');", kafkaMessage[i].offset, kafkaMessage[i].message, kafkaMessage[i].topic, kafkaMessage[i].partition), sqlCon))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                return Json("success");
            }
            catch (Exception exception)
            {
                return Json(exception.Message);
            }
        }

        public IActionResult Error()
        {
            return View();
        }

        private int getLastKafkaOffset()
        {
            var offset = -1;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("select coalesce(max(\"Offset\"),0) as maxoffset from fxvip;");
                using (var sqlCon = new NpgsqlConnection(ConfigVars.Instance.PostgresDBConnectionString))
                {
                    sqlCon.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand(sb.ToString(), sqlCon))
                    {
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int.TryParse(reader["maxoffset"].ToString(), out offset);
                                    ViewBag.LastOffset = offset;
                                }

                                if (reader.NextResult() == false)
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return offset;
        }
    }
}
