using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HueBridge.Models
{
    public class Group
    {
        public int Id { get; set; }
        public List<int> Lights { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Class { get; set; }
    }
}
