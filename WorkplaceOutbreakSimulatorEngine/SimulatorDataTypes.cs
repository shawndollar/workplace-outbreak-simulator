using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{

    public static class SimulatorDataConstant
    {
        public const string EmployeeGender_M = "M";
        public const string EmployeeGender_F = "F";
        public const string WorkplaceRoomType_Office = "Office";
        public const string WorkplaceRoomType_Breakroom = "Breakroom";
        public const string WorkplaceRoomType_Meeting = "Meeting";
        public const string InfectionStage_Well = "Well";
        public const string InfectionStage_Infected = "Infected";
        public const string InfectionStage_Incubation = "Incubation";
        public const string InfectionStage_Symptomatic = "Symptomatic";
        public const string InfectionStage_Immune = "Immune";
        public const string InfectionTestResult_Pending = "Pending";
        public const string InfectionTestResult_Positive = "Positive";
        public const string InfectionTestResult_Negative = "Negative";
    }
}