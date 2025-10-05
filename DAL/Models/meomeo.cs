using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
   public class meomeo
    {
        [Key]
        public int MeoId
        { get; set; }
        public string meo { get; set; } = string.Empty;
        public string meo2 { get; set; } = string.Empty;
    }
}
