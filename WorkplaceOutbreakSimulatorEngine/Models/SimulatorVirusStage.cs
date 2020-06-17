using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorVirusStage
    {

        public SimulatorVirusStage(int virusId, int stageOrder, string infectionStage, int minDays, int maxDays)
        {
            VirusId = virusId;
            StageOrder = stageOrder;
            InfectionStage = infectionStage;
            MinDays = minDays;
            MaxDays = maxDays;
        }

        public int Id { get; set; }
        public int VirusId { get; set; }
        public int StageOrder { get; set; }
        public string InfectionStage { get; set; }
        public int MinDays { get; set; }
        public int MaxDays { get; set; }
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

        public override string ToString()
        {
            return $"{Id} {VirusId} {InfectionStage} {StageOrder}";
        }

    }
}