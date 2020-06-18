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
        public SimulatorOutput SimulatorOutput { get; set; }
    }
}
