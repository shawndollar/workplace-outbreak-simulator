using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorResult
    {
        IList<SimulatorEmployeeContact> EmployeeContacts { get; set; } = new List<SimulatorEmployeeContact>();

        public bool HasError { get; set; }

        public string ErrorMessage { get; set; }
    }
}
