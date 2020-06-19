using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WorkplaceOutbreakSimulatorWebApp.Model
{
    public class SimulatorData
    {
        public bool IsSimulatorRunning { get; set; }

        public bool IsSimulatorComplete { get; set; }
        
        public SimulatorInput SimulatorInput { get; set; }

        [Range(1, 1, ErrorMessage = "You must select an employee to download an employee contact log.")]
        public int IsEmployeeSelectedForExport
        {
            get
            {
                int i;
                if (IsSimulatorComplete)
                {
                    // If simulation is complete, we must have a selected employee for export.
                    if (string.IsNullOrWhiteSpace(SelectedEmployeeIdForExport) ||
                        !Int32.TryParse(SelectedEmployeeIdForExport, out i) ||
                        i == 0)
                    {
                        return 0;
                    }
                }
                return 1;
            }
        }

        public string SelectedEmployeeIdForExport { get; set; }
    }
}
