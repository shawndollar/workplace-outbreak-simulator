using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorEmployee
    {

        public int Id { get; set; }

        public int RoomId { get; set; }               

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Gender { get; set; }

        public bool IsBreakroomUser { get; set; }

        public int VirusStageId { get; set; }

        public DateTime? VirusStageLastChangeDateTime
        {
            get; set;
        }

        public DateTime? SickLeaveDateTime { get; set; }


        public override string ToString()
        {
            return $"{Id}, {FirstName} {LastName}, {Gender}, {RoomId}";
        }
    }
}
