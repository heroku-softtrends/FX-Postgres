using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FXPostgres.Models
{
    public class KafkaMessage
    {
        public string message { get; set; }
        public string topic { get; set; }
        public int offset { get; set; }
        public int partition { get; set; }
    }
}
