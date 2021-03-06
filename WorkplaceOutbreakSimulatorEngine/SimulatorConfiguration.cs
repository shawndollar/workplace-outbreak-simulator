﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine
{
    public class SimulatorConfiguration
    {
        public TimeSpan StartOfWorkday { get; set; }

        public TimeSpan EndOfWorkday { get; set; }

        public DateTime StartDateTime { get; set; }

        public DateTime EndDateTime { get; set; }

        public TimeSpan DataInterval { get; set; }

        public TimeSpan MeetingTimeSpan { get; set; }

        public int MinMeetingAttendance { get; set; }

        public int MaxMeetingAttendance { get; set; }

        public TimeSpan BreakTimeOfDay { get; set; }

        public TimeSpan BreakTimeSpan { get; set; }

        public int InitialSickCount { get; set; }

        public string InitialSickStage { get; set; }

        public IDictionary<int, int> FloorPeopleMapping { get; set; }

        public int TotalPeople
        {
            get
            {
                return FloorPeopleMapping?.Sum(f => f.Value) ?? 0;
            }
        }

        public IList<SimulatorEmployee> Employees { get; set; } = new List<SimulatorEmployee>();

        public SimulatorWorkplace Workplace { get; set; }

        public IList<SimulatorWorkplaceFloor> WorkplaceFloors { get; set; } = new List<SimulatorWorkplaceFloor>();

        public IList<SimulatorWorkplaceRoom> WorkplaceRooms { get; set; } = new List<SimulatorWorkplaceRoom>();
        
        public SimulatorVirus Virus { get; set; }

        public IList<SimulatorVirusStage> VirusStages { get; set; } = new List<SimulatorVirusStage>();

        public bool CanGetSickInOffice { get; set; }

    }
}