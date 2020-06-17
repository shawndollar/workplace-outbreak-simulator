using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorEmployeeContact
    {
        public DateTime ContactDateTime { get; set; }
        public int EmployeeId { get; set; }
        public int RoomId { get; set; }
        public int VirusStageId { get; set; }
        public int ContactEmployeeId { get; set; }                
    }
}
