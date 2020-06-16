using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorVirusStage
    {
        public int Id { get; set; }
        public int VirusId { get; set; }
        public int StageOrder { get; set; }
        public string InfectionStage { get; }
        public int MinDays { get; }
        public int MaxDays { get; }
        public bool IsContagious
        {
            get
            {
                return InfectionStage == SimulatorDataConstant.InfectionStage_Infected ||
                    InfectionStage == SimulatorDataConstant.InfectionStage_Incubation ||
                    InfectionStage == SimulatorDataConstant.InfectionStage_Symptomatic;
            }
        }
        public bool IsInfected
        {
            get
            {
                return InfectionStage == SimulatorDataConstant.InfectionStage_Incubation ||
                    InfectionStage == SimulatorDataConstant.InfectionStage_Infected ||
                    InfectionStage == SimulatorDataConstant.InfectionStage_Symptomatic;
            }
        }
        public bool IsSick
        {
            get
            {
                return InfectionStage == SimulatorDataConstant.InfectionStage_Symptomatic;
            }
        }

    }
}