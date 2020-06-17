using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorResult
    {
        public IList<SimulatorEmployeeContact> EmployeeContacts { get; set; } = new List<SimulatorEmployeeContact>();

        public DateTime? CompleteInfectionDateTime { get; set; }

        public bool HasError { get; set; }

        public string ErrorMessage { get; set; }

        public bool IsSimulatorComplete { get; set; }

    }
}
