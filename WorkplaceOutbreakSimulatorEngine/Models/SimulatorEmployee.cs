using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorEmployee
    {
        public SimulatorWorkplaceRoom WorkplaceRoom { get; set; }

        public SimulatorEmployeeGender Gender { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Id { get; set; }
    }
}
