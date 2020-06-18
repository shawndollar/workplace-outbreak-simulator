using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine.DataRepository
{
    public static class SimulatorConfigManager
    {
        #region Methods

        public static SimulatorConfiguration GetDefaultConfiguration()
        {
            SimulatorConfiguration configuration = new SimulatorConfiguration();

            // Simulation will collect data every one hour.
            configuration.DataInterval = new TimeSpan(1, 0, 0);

            // Set workday start/end time
            configuration.StartOfWorkday = new TimeSpan(8, 0, 0);
            configuration.EndOfWorkday = new TimeSpan(17, 0, 0);

            // Set start of simulation to the current date.
            // Begin with start of workday on first day and end at end of workday on last day.
            configuration.StartDateTime = GetDateBoundary(DateTime.Now, configuration.StartOfWorkday);

            // Four month time span and set the time to the end of the work day.
            configuration.EndDateTime = GetDateBoundary(configuration.StartDateTime.AddMonths(4), configuration.EndOfWorkday);
            
            // Number of people to be sick initially.
            configuration.InitialSickCount = 1;

            // Set initial infected stage of the sick people.
            configuration.InitialSickStage = SimulatorDataConstant.InfectionStage_Infected;

            // Set meeting time span to 1 hour.
            configuration.MeetingTimeSpan = new TimeSpan(1, 0, 0);

            // Set min and max meeting attendance numbers.
            configuration.MinMeetingAttendance = 4;
            configuration.MaxMeetingAttendance = 8;

            // Set the break time hours to 12-1pm
            configuration.BreakTimeOfDay = new TimeSpan(12, 0, 0);
            configuration.BreakTimeSpan = new TimeSpan(1, 0, 0);

            // Get default workplace, virus, and virus stages.
            configuration.Workplace = GetDefaultWorkplace();
            configuration.Virus = GetDefaultVirus();
            configuration.VirusStages = GetDefaultVirusStages(configuration.Virus.Id);

            // Just set default workplace floors and people per floor.            
            configuration.FloorPeopleMapping = GetDefaultFloorPeopleMapping();
            configuration.WorkplaceFloors = GetWorkplaceFloors(configuration.Workplace.Id, configuration.FloorPeopleMapping.Count);

            // add all the rooms as requested
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 1).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Office);

            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 12, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 15, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 5, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 3, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 50, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(configuration.WorkplaceRooms, configuration.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            // Just leave empty for now.
            configuration.Employees = new List<SimulatorEmployee>();

            return configuration;
        }

        public static SimulatorWorkplace GetDefaultWorkplace()
        {
            var wp = new SimulatorWorkplace();
            wp.Id = 1;
            wp.Name = "Default";
            return wp;
        }

        public static SimulatorVirus GetDefaultVirus()
        {
            SimulatorVirus virus = new SimulatorVirus();
            virus.InfectionRate = .2m;
            virus.TestRate = .35m;
            virus.RecoveryDays = 5;
            virus.Id = 1;
            virus.TestResultWaitTime = new TimeSpan(3, 0, 0, 0);
            return virus;
        }

        public static IList<SimulatorVirusStage> GetDefaultVirusStages(int virusId)
        {
            IList<SimulatorVirusStage> virusStages = new List<SimulatorVirusStage>();

            int order = 1;

            virusStages.Add(new SimulatorVirusStage(virusId, order++, SimulatorDataConstant.InfectionStage_Well, 0, 0));
            virusStages.Add(new SimulatorVirusStage(virusId, order++, SimulatorDataConstant.InfectionStage_Infected, 3, 3));
            virusStages.Add(new SimulatorVirusStage(virusId, order++, SimulatorDataConstant.InfectionStage_Incubation, 3, 7));
            virusStages.Add(new SimulatorVirusStage(virusId, order++, SimulatorDataConstant.InfectionStage_Symptomatic, 6, 11));
            virusStages.Add(new SimulatorVirusStage(virusId, order++, SimulatorDataConstant.InfectionStage_Immune, 0, 0));

            int id = 1;
            foreach (var vs in virusStages.OrderBy(f => f.StageOrder))
            {
                vs.Id = id++;
            }

            return virusStages;
        }

        public static void AddRoomsToFloor(IList<SimulatorWorkplaceRoom> rooms, int floorId, int numberOfRooms, string roomType)
        {
            for (int i = 0; i < numberOfRooms; i++)
            {
                var room = new SimulatorWorkplaceRoom(floorId, roomType);
                int maxRoomId = (rooms.Count == 0) ? 0 : rooms.Max(f => f.Id);
                room.Id = maxRoomId + 1;
                rooms.Add(room);
            }
        }

        public static IList<SimulatorWorkplaceFloor> GetWorkplaceFloors(int workPlaceId, int numberOfFloors)
        {
            var floors = new List<SimulatorWorkplaceFloor>();
            for (int i = 1; i <= numberOfFloors; i++)
            {
                floors.Add(new SimulatorWorkplaceFloor(i, workPlaceId, i));
            }
            return floors;
        }

        public static IDictionary<int, int> GetDefaultFloorPeopleMapping()
        {
            int floorCount = 5;
            IDictionary<int, int> floorNumberPeopleCountDict = new Dictionary<int, int>(floorCount);
            floorNumberPeopleCountDict.Add(1, 30);
            floorNumberPeopleCountDict.Add(2, 150);
            floorNumberPeopleCountDict.Add(3, 150);
            floorNumberPeopleCountDict.Add(4, 150);
            floorNumberPeopleCountDict.Add(5, 50);
            return floorNumberPeopleCountDict;
        }

        public static void SetEmployeesBreakroomUse(SimulatorConfiguration configuration, decimal breakroomPercentage)
        {
            // Convert truncates, so we'll round first, but the rounding is not really necessary.
            int breakRoomUserCount = Convert.ToInt32(Math.Round(configuration.Employees.Count * breakroomPercentage, 0, MidpointRounding.AwayFromZero));

            // Just set the first employees in the list. It's still random. We don't know where these employees will end up (necessarily).
            foreach (var item in configuration.Employees)
            {
                item.IsBreakroomUser = breakRoomUserCount > 0 ? true : false;
                breakRoomUserCount--;
            }
        }

        /// <summary>
        ///  Randomly assign employees to a random office on a random floor, while ensuring that each floor has the correct number of employees.
        ////  All of the other configuration properties should be set before you do this. This should be the last step in setting up the configuration.
        /// </summary>
        /// <param name="configuration">The configuration to modify.</param>
        public static void AssignEmployeesToOffices(SimulatorConfiguration configuration)
        {
            Random random = new Random();

            // get list of all floors that have occupancy. all floor numbers must exist in the dictionary.
            IList<int> unfilledFloors = (from f in configuration.WorkplaceFloors
                                         where configuration.FloorPeopleMapping[f.FloorNumber] > 0
                                         select f.FloorNumber).OrderBy(f => f).ToList();

            foreach (var employee in configuration.Employees)
            {
                int floorNumber = unfilledFloors[random.Next(0, unfilledFloors.Count)];
                int floorId = configuration.WorkplaceFloors.FirstOrDefault(f => f.FloorNumber == floorNumber).Id;
                IList<SimulatorWorkplaceRoom> officesOnFloor = configuration.WorkplaceRooms.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office && f.FloorId == floorId).ToList();
                int roomId = officesOnFloor[random.Next(0, officesOnFloor.Count)].Id;
                employee.OfficeId = roomId;
                int peopleOnFloor = (from e in configuration.Employees
                                     join r in configuration.WorkplaceRooms on e.OfficeId equals r.Id
                                     where r.FloorId == floorId
                                     select e).Count();
                if (peopleOnFloor >= configuration.FloorPeopleMapping[floorNumber])
                {
                    unfilledFloors.Remove(floorNumber);
                }
            }
        }

        public static DateTime GetDateBoundary(DateTime startDate, TimeSpan timeSpan)
        {
            return new DateTime(startDate.Year, startDate.Month, startDate.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
        }

        #endregion Methods

    }
}