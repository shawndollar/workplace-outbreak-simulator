using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorInfectionTest
    {

        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string InfectiontTestResult { get; set; }

        public DateTime? InfectionTestDateTime { get; set; }
    
    }
}
