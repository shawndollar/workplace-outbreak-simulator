using System;
using System.Collections.Generic;
using System.Text;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine
{
    public class SimulatorConfiguration
    {
        public IList<SimulationEmployee> Employees { get; set; }

        public SimulationWorkplace Workplace { get; set; }

        public SimulationTimeframe Timeframe { get; set; }

        public SimulationVirus Virus { get; set; }

        public SimulationWorkday Workday { get; set; }
    }
}