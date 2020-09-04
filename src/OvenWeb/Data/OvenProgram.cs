using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OvenWeb.Data
{
    public class OvenProgram
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CurrentItemId { get; set; }
        public List<OvenProgramItem> Items { get; set; }
    }

    public class OvenProgramItem
    {
        public int Id { get; set; }
        public int Temperature { get; set; }
        public DateTime Time { get; set; }
        public string TimeString { get; set; }
        public int Relays { get; set; } = 1;
    }
}
