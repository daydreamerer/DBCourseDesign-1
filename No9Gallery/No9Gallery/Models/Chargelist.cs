using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace No9Gallery.Models
{
    public class Chargelist
    {
        [Display(Name = "order no")]
        public string order_no { get; set; }

        public string user_ID { get; set; }
        public int amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime time { get; set; }
        public string content { get; set; }

    }
}
