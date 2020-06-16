using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorWorkplaceRoom
    {
        public int Id { get; set; }

        public int FloorId { get; set; }

        public int RoomNumber { get; set; }

        public string RoomType { get; set; }
    }
}
