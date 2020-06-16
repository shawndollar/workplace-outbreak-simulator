using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorVirus
    {

        public int Id { get; set; }

        public decimal InfectionRate { get; set; }

        public decimal TestRate { get; set; }

        public int RecoveryDays { get; set; }

    }
}
