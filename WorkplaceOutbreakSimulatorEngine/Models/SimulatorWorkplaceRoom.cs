using System;
using System.Collections.Generic;
using System.Text;

namespace WorkplaceOutbreakSimulatorEngine.Models
{
    public class SimulatorWorkplaceRoom
    {

        public SimulatorWorkplaceRoom(int floorId, string roomType)
        {
            FloorId = floorId;
            RoomType = roomType;
        }

        public int Id { get; set; }

        public int FloorId { get; set; }
        
        public string RoomType { get; set; }

        public override string ToString()
        {
            return $"{Id}, {FloorId}, {RoomType}";
        }
    }
}
