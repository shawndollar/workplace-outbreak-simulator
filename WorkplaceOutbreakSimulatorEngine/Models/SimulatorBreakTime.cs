using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorBreakTime
    {

        public int Id { get; set; }

        public int RoomId { get; set; }

        public IList<SimulatorEmployee> Employees { get; set; } = new List<SimulatorEmployee>();

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

    }
}
