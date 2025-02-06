using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graveyard.Models
{
    public class HistoricTagModel
    {
        public int Id { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
