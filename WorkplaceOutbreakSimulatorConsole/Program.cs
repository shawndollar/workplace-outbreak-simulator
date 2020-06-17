using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorEngine.Models;
using System.Text.Json;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;

namespace WorkplaceOutbreakSimulatorConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            //do
            //{
            //    totalHours++;
            //    startDate = startDate.Add(ts);
            //    if (startDate.TimeOfDay >= simConfig.StartOfWorkday &&
            //        startDate.TimeOfDay <= simConfig.EndOfWorkday)
            //    {
            //        workHours++;
            //        Console.WriteLine(startDate.ToString("yyyy-MM-dd h:mm:ss tt"));
            //    }
            //} while (startDate < endDate);
                        
            SimulatorConfiguration simConfig = await CreateConfiguration();

            SimulatorEngine simEngine = new SimulatorEngine(simConfig);

            // how many people on each floor
            foreach (var floor in simConfig.WorkplaceFloors)
            {
                int officesOnFloor = simConfig.WorkplaceRooms.Count(f => f.FloorId == floor.Id && f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office);
                int peopleOnFloor = 0;
                
                Console.WriteLine($"Floor {floor.FloorNumber} Offices {officesOnFloor}");

                foreach (var office in simConfig.WorkplaceRooms.Where(f => f.FloorId == floor.Id && f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office))
                {
                    int peopleInOffice = 0;
                    peopleInOffice = simConfig.Employees.Count(f => f.RoomId == office.Id);
                    Console.WriteLine($"Office {office.Id} People {peopleInOffice}");                    
                    peopleOnFloor += peopleInOffice;
                }
                Console.WriteLine($"People On Floor {floor.Id} {peopleOnFloor}");
                Console.WriteLine("");
            }

            Console.ReadKey();

        }

        static async Task<SimulatorConfiguration> CreateConfiguration()
        {
            SimulatorDataStore simDataStore = new SimulatorDataStore("https://api.mockaroo.com/api/f028dfc0", "89c948e0");

            SimulatorConfiguration simConfig = new SimulatorConfiguration();

            simConfig.DataInterval = new TimeSpan(1, 0, 0); // simulation will collect data every one hour

            // set workday start/end time
            simConfig.StartOfWorkday = new TimeSpan(8, 0, 0);
            simConfig.EndOfWorkday = new TimeSpan(17, 0, 0);

            // set start and end date of simulation
            // begin with start of workday on first day and end at end of workday on last day
            {
                DateTime now = DateTime.Now;
                simConfig.StartDateTime = new DateTime(now.Year, now.Month, now.Day, simConfig.StartOfWorkday.Hours, simConfig.StartOfWorkday.Minutes, simConfig.StartOfWorkday.Seconds);
            }
            simConfig.EndDateTime = simConfig.StartDateTime.AddMonths(4);
            simConfig.EndDateTime = new DateTime(simConfig.EndDateTime.Year, simConfig.EndDateTime.Month, simConfig.EndDateTime.Day, simConfig.EndOfWorkday.Hours, simConfig.EndOfWorkday.Minutes, simConfig.EndOfWorkday.Seconds);

            simConfig.InitialSickCount = 1; // start with one sick person
            simConfig.InitialSickStage = SimulatorDataConstant.InfectionStage_Symptomatic; // start person off with symptoms

            simConfig.MeetingTimeSpan = new TimeSpan(1, 0, 0); // meetings are one-hour

            simConfig.MinMeetingAttendance = 4;
            simConfig.MaxMeetingAttendance = 8;

            simConfig.BreakTimeOfDay = new TimeSpan(12, 0, 0); // this is the time at which employees can take a break in the breakrooms
            simConfig.BreakTimeSpan = new TimeSpan(1, 0, 0); // this is the length of break time

            simConfig.Workplace = GetWorkplace();

            int floorCount = 5;
            IDictionary<int, int> floorNumberPeopleCountDict = new Dictionary<int, int>(floorCount);
            floorNumberPeopleCountDict.Add(1, 30);
            floorNumberPeopleCountDict.Add(2, 150);
            floorNumberPeopleCountDict.Add(3, 150);
            floorNumberPeopleCountDict.Add(4, 150);
            floorNumberPeopleCountDict.Add(5, 50);

            simConfig.WorkplaceFloors = GetWorkplaceFloors(simConfig.Workplace.Id, floorCount); // add 5 floors, assume contiguous

            // add all the rooms as requested
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 1).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Office);
            
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 12, SimulatorDataConstant.WorkplaceRoomType_Office);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 15, SimulatorDataConstant.WorkplaceRoomType_Office);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 5, SimulatorDataConstant.WorkplaceRoomType_Office);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 3, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 50, SimulatorDataConstant.WorkplaceRoomType_Office);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            simConfig.WorkplaceRooms.AddRooms(simConfig.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            simConfig.Employees = await GetEmployeesAsync(simDataStore, 530, false);

            AssignEmployeesToOffices(simDataStore, simConfig.WorkplaceFloors, simConfig.WorkplaceRooms.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office).ToList(), floorNumberPeopleCountDict, simConfig.Employees);

            return simConfig;

        }

        static SimulatorWorkplace GetWorkplace()
        {
            var wp = new SimulatorWorkplace();
            wp.Id = 1;
            wp.Name = "Default";
            return wp;
        }
        
        static IList<SimulatorWorkplaceFloor> GetWorkplaceFloors(int workPlaceId, int numberOfFloors)
        {
            var floors = new List<SimulatorWorkplaceFloor>();
            for (int i = 1; i <= numberOfFloors; i++)
            {
                floors.Add(new SimulatorWorkplaceFloor(i, workPlaceId, i));
            }
            return floors;
        }

        /// <summary>
        ///  randomly assign employees to a random office on a random floor, while ensuring that each floor has the correct number of employees
        /// </summary>
        /// <param name="simDataStore"></param>
        /// <param name="floors"></param>
        /// <param name="offices"></param>
        /// <param name="floorPeopleCountDict"></param>
        /// <param name="employees"></param>
        static void AssignEmployeesToOffices(SimulatorDataStore simDataStore, IList<SimulatorWorkplaceFloor> floors, IList<SimulatorWorkplaceRoom> offices, IDictionary<int, int> floorNumberPeopleCountDict, IList<SimulatorEmployee> employees)
        {
            // get list of all floors that have occupancy. all floor numbers must exist in the dictionary.
            IList<int> unfilledFloors = (from f in floors
                                         where floorNumberPeopleCountDict[f.FloorNumber] > 0
                                         select f.FloorNumber).OrderBy(f => f).ToList();

            foreach (var employee in employees)
            {
                int floorNumber = unfilledFloors[simDataStore.GetRandomNumber(0, unfilledFloors.Count)];
                int floorId = floors.FirstOrDefault(f => f.FloorNumber == floorNumber).Id;
                IList<SimulatorWorkplaceRoom> officesOnFloor = offices.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office && f.FloorId == floorId).ToList();
                int roomId = officesOnFloor[simDataStore.GetRandomNumber(0, officesOnFloor.Count)].Id;
                employee.RoomId = roomId;
                int peopleOnFloor = (from e in employees
                                  join r in offices on e.RoomId equals r.Id
                                  where r.FloorId == floorId
                                  select e).Count();
                if (peopleOnFloor >= floorNumberPeopleCountDict[floorNumber])
                {
                    unfilledFloors.Remove(floorNumber);
                }
            }
        }

        static async Task<IList<SimulatorEmployee>> GetEmployeesAsync(SimulatorDataStore simDataStore, int count, bool getFromFile)
        {
            string employeesJson = null;

            if (!getFromFile)
            {
                try
                {
                    employeesJson = await simDataStore.GetEmployeesAsync(530);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc);
                }
            }

            if (string.IsNullOrWhiteSpace(employeesJson))
            {
                employeesJson = await simDataStore.GetEmployeesFromFileAsync(@"C:\projects\repos\workplace-outbreak-simulator-repo\employees.json");
            }

            var results = JsonSerializer.Deserialize<IList<SimulatorEmployee>>(employeesJson);
            if (results.Count > count)
            {
                results = results.Take(count).ToList();
            }
            return results;
        }

    }

    public static class ExtensionMethods
    {
        public static void AddRooms(this IList<SimulatorWorkplaceRoom> rooms, int floorId, int numberOfRooms, string roomType)
        {
            for (int i = 0; i < numberOfRooms; i++)
            {
                var room = new SimulatorWorkplaceRoom(floorId, roomType);
                int maxRoomId = (rooms.Count == 0) ? 0 : rooms.Max(f => f.Id);
                room.Id = maxRoomId + 1;
                rooms.Add(room);
            }
        }
    }
}