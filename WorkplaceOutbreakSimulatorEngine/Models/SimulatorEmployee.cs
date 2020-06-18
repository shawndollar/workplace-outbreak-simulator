using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorEmployee
    {

        public int Id { get; set; }

        public int OfficeId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                return $"{FirstName ?? ""} {LastName ?? ""}".Trim();
            }
        }

        public string Gender { get; set; }

        public bool IsBreakroomUser { get; set; }

        public int VirusStageId { get; set; }

        public DateTime? VirusStageLastChangeDateTime
        {
            get; set;
        }

        public DateTime? ScheduledVirusStageChangeDateTime
        {
            get;
            set;
        }

        public DateTime? SickLeaveStartDateTime { get; set; }

        public bool IsOutSick
        {
            get
            {
                return SickLeaveStartDateTime != null;
            }
        }

        public string InfectiontTestResult { get; set; }

        public DateTime? InfectionTestDateTime { get; set; }

        public bool DoNotTest { get; set; }

        public int? CurrentRoomId { get; set; }

        public override string ToString()
        {
            return $"{Id}, {FullName}, {Gender}, {OfficeId}";
        }
    }
}
