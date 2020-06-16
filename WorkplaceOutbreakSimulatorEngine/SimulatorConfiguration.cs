using System;
using System.Collections.Generic;
using System.Text;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine
{
    public class SimulatorConfiguration
    {
        public IList<SimulatorEmployee> Employees { get; set; }

        public SimulatorWorkplace Workplace { get; set; }

        public SimulatorTimeframe Timeframe { get; set; }

        public SimulatorVirus Virus { get; set; }

        public SimulatorWorkday Workday { get; set; }
    }
}