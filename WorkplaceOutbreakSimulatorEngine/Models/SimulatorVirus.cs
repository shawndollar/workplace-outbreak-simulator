using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorVirus
    {

        public SimulatorVirus(decimal infectionRate, decimal testRate, int recoveryDays)
        {
            InfectionRate = infectionRate;
            TestRate = testRate;
            RecoveryDays = recoveryDays;
        }

        public int Id { get; set; }

        public decimal InfectionRate { get; set; }

        public decimal TestRate { get; set; }

        public int RecoveryDays { get; set; }

        public override string ToString()
        {
            return $"{Id} {InfectionRate} {TestRate} {RecoveryDays}";
        }

    }
}
