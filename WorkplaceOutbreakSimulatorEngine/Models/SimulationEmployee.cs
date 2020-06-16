using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulationEmployee
    {
        public SimulationWorkplaceRoom WorkplaceRoom { get; set; }

        public SimulationEmployeeGender Gender { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public int Id { get; set; }
    }
}
