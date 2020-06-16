using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorWorkplaceFloor
    {
        public SimulatorWorkplaceFloor(int id, int workplaceId, int floorNumber)
        {
            Id = id;
            WorkplaceId = workplaceId;
            FloorNumber = floorNumber;
        }

        public int Id { get; set; }                

        public int WorkplaceId { get; set; }

        public int FloorNumber { get; set; }

    }
}