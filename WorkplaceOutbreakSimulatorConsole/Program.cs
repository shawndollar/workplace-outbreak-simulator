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
using System.IO;
using WorkplaceOutbreakSimulatorEngine.Helpers;
using CsvHelper;
using System.Globalization;
using System.Dynamic;
using System.ComponentModel.DataAnnotations;

namespace WorkplaceOutbreakSimulatorConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            bool useTestPersonFile = true;
            
            string outputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "simulator_output.txt");
            
            string csvOutputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "simulator_output.csv");
            string csvOutputFile2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "simulator_output2.csv");

            StreamWriter sw = new StreamWriter(outputFile);

            SimulatorConfiguration simConfig = await CreateConfiguration(useTestPersonFile);
            SimulatorEngine simEngine = new SimulatorEngine(simConfig);
            IList<SimulatorEmployeeContact> allEmployeeContacts = new List<SimulatorEmployeeContact>();
            SimulatorResult simulatorResult = new SimulatorResult();

            try
            {                
                simEngine.InitializeSimulation();
                sw.WriteLine("RUN " + DateTime.Now);

                do
                {
                    simulatorResult = simEngine.RunNext();

                    //sw.WriteLine(simEngine.SimulatorDateTime + " Infected: " + (from e in simConfig.Employees
                    //                                                            join v in simConfig.VirusStages
                    //                                                            on e.VirusStageId equals v.Id
                    //                                                            where v.IsInfected
                    //                                                            select e).Count() + " Immune: " +
                    //                                                            (from e in simConfig.Employees
                    //                                                             join v in simConfig.VirusStages
                    //                                                             on e.VirusStageId equals v.Id
                    //                                                             where v.InfectionStage ==
                    //                                                             SimulatorDataConstant.InfectionStage_Immune
                    //                                                             select e).Count());
                    allEmployeeContacts.AddRange(simulatorResult.EmployeeContacts);
                }
                while (!simulatorResult.IsSimulatorComplete && !simulatorResult.HasError);

                sw.WriteLine("Status " + DateTime.Now + " " + !simulatorResult.HasError + " " + simulatorResult.ErrorMessage);
            }
            finally
            {
                sw.Close();
            }

            //await CreateSimulatorCsvLogAsync(allEmployeeContacts, simConfig.Employees, simConfig.WorkplaceRooms, simConfig.VirusStages, csvOutputFile);
            //await CreateSimulatorCsvLogAsync(allEmployeeContacts.Where(f => f.EmployeeId == allEmployeeContacts[0].EmployeeId).ToList(), simConfig.Employees, simConfig.WorkplaceRooms, simConfig.VirusStages, csvOutputFile2);
            //await CreateSimulatorCsvLogAsync(allEmployeeContacts.Where(f => f.EmployeeId == simConfig.Employees.First(e => e.VirusStageId < 5).Id).ToList(), simConfig.Employees, simConfig.WorkplaceRooms, simConfig.VirusStages, csvOutputFile2);
        }

        /// <summary>
        /// Create and write the employee contact info to a CSV file.
        /// </summary>
        /// <param name="contacts">The records to write out to the CSV file.</param>
        /// <param name="employees">The full list of employees for looking up stuff.</param>
        /// <param name="rooms">The full list of rooms for looking up room info.</param>
        /// <param name="virusStages">The full list of virus stages for looking up virus stage info.</param>
        /// <param name="outputFile">The output file to write to.</param>
        /// <returns>Task with no return value.</returns>
        static async Task CreateSimulatorCsvLogAsync(IList<SimulatorEmployeeContact> contacts, 
            IList<SimulatorEmployee> employees, 
            IList<SimulatorWorkplaceRoom> rooms, 
            IList<SimulatorVirusStage> virusStages, 
            string outputFile)
        {
            var dynamicList = new List<dynamic>(contacts.Count);

            foreach (var contact in contacts)
            {
                var employee = employees.FirstOrDefault(f => f.Id == contact.EmployeeId);
                var virusStage = virusStages.FirstOrDefault(f => f.Id == contact.VirusStageId);
                var workplaceRoom = rooms.FirstOrDefault(f => f.Id == contact.RoomId);
                dynamic d = new ExpandoObject();
                d.ContactDateTime = contact.ContactDateTime;
                d.EmployeeId = employee?.Id;
                d.Name = employee?.FullName;
                // d.Gender = employee?.Gender;
                d.RoomId = contact.RoomId;
                d.RoomType = (workplaceRoom == null) ? "None" : workplaceRoom.RoomType;
                d.VirusStatus = virusStage?.InfectionStage;
                for (int i = 0; i < contact.EmployeeContacts?.Count; i++)
                {
                    var contactEmployee = employees.FirstOrDefault(f => f.Id == contact.EmployeeContacts[i].EmployeeId);
                    AddDynamicProperty(d, $"Contact{i + 1}", contactEmployee.FullName);
                }
                dynamicList.Add(d);
            }

            using (var writer = new StreamWriter(outputFile))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    await csv.WriteRecordsAsync(dynamicList);
                }
            }
        }

        /// <summary>
        /// Use IDictionary methods of ExpandoObject to add properties.
        /// </summary>
        /// <param name="expando">The ExpandObject class to add a property</param>
        /// <param name="propertyName">The name of the new property.</param>
        /// <param name="propertyValue">The object of the new property.</param>
        static void AddDynamicProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
            {
                expandoDict[propertyName] = propertyValue;
            }
            else
            {
                expandoDict.Add(propertyName, propertyValue);
            }
        }

        static async Task<SimulatorConfiguration> CreateConfiguration(bool useTestPersonFile)
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
            simConfig.InitialSickStage = SimulatorDataConstant.InfectionStage_Infected; // set initial infected stage

            simConfig.MeetingTimeSpan = new TimeSpan(1, 0, 0); // meetings are one-hour

            simConfig.MinMeetingAttendance = 4;
            simConfig.MaxMeetingAttendance = 8;

            simConfig.BreakTimeOfDay = new TimeSpan(12, 0, 0); // this is the time at which employees can take a break in the breakrooms
            simConfig.BreakTimeSpan = new TimeSpan(1, 0, 0); // this is the length of break time

            simConfig.Workplace = GetWorkplace();

            simConfig.Virus = GetVirus();
            simConfig.VirusStages = GetVirusStages(simConfig.Virus.Id);

            int floorCount = 5;
            IDictionary<int, int> floorNumberPeopleCountDict = new Dictionary<int, int>(floorCount);
            floorNumberPeopleCountDict.Add(1, 30);
            floorNumberPeopleCountDict.Add(2, 150);
            floorNumberPeopleCountDict.Add(3, 150);
            floorNumberPeopleCountDict.Add(4, 150);
            floorNumberPeopleCountDict.Add(5, 50);

            simConfig.WorkplaceFloors = GetWorkplaceFloors(simConfig.Workplace.Id, floorCount); // add 5 floors, assume contiguous

            // add all the rooms as requested
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 1).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Office);

            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 12, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 2).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 15, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 3).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 5, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 4).Id, 3, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 50, SimulatorDataConstant.WorkplaceRoomType_Office);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 1, SimulatorDataConstant.WorkplaceRoomType_Breakroom);
            AddRoomsToFloor(simConfig.WorkplaceRooms, simConfig.WorkplaceFloors.First(f => f.FloorNumber == 5).Id, 2, SimulatorDataConstant.WorkplaceRoomType_Meeting);

            simConfig.Employees = await GetEmployeesAsync(simDataStore, floorNumberPeopleCountDict.Sum(f => f.Value), useTestPersonFile);

            // mark employees who will use break room
            SetEmployeesBreakroomUse(simConfig.Employees, .25m);

            AssignEmployeesToOffices(simConfig.WorkplaceFloors, simConfig.WorkplaceRooms.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office).ToList(), floorNumberPeopleCountDict, simConfig.Employees);

            return simConfig;
        }

        static void SetEmployeesBreakroomUse(IList<SimulatorEmployee> employees, decimal breakroomPercentage)
        {
            // convert truncates fractional digits, so let's go ahead and round first
            int breakRoomUserCount = Convert.ToInt32(Math.Round(employees.Count * breakroomPercentage, 0, MidpointRounding.AwayFromZero)); 

            foreach(var item in employees)
            {
                item.IsBreakroomUser = breakRoomUserCount > 0 ? true : false;
                breakRoomUserCount--;
            }
        }

        static SimulatorWorkplace GetWorkplace()
        {
            var wp = new SimulatorWorkplace();
            wp.Id = 1;
            wp.Name = "Default";
            return wp;
        }

        static void AddRoomsToFloor(IList<SimulatorWorkplaceRoom> rooms, int floorId, int numberOfRooms, string roomType)
        {
            for (int i = 0; i < numberOfRooms; i++)
            {
                var room = new SimulatorWorkplaceRoom(floorId, roomType);
                int maxRoomId = (rooms.Count == 0) ? 0 : rooms.Max(f => f.Id);
                room.Id = maxRoomId + 1;
                rooms.Add(room);
            }
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
        static void AssignEmployeesToOffices(IList<SimulatorWorkplaceFloor> floors, IList<SimulatorWorkplaceRoom> offices, IDictionary<int, int> floorNumberPeopleCountDict, IList<SimulatorEmployee> employees)
        {
            Random random = new Random();

            // get list of all floors that have occupancy. all floor numbers must exist in the dictionary.
            IList<int> unfilledFloors = (from f in floors
                                         where floorNumberPeopleCountDict[f.FloorNumber] > 0
                                         select f.FloorNumber).OrderBy(f => f).ToList();

            foreach (var employee in employees)
            {
                int floorNumber = unfilledFloors[random.Next(0, unfilledFloors.Count)];
                int floorId = floors.FirstOrDefault(f => f.FloorNumber == floorNumber).Id;
                IList<SimulatorWorkplaceRoom> officesOnFloor = offices.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Office && f.FloorId == floorId).ToList();
                int roomId = officesOnFloor[random.Next(0, officesOnFloor.Count)].Id;
                employee.OfficeId = roomId;
                int peopleOnFloor = (from e in employees
                                  join r in offices on e.OfficeId equals r.Id
                                  where r.FloorId == floorId
                                  select e).Count();
                if (peopleOnFloor >= floorNumberPeopleCountDict[floorNumber])
                {
                    unfilledFloors.Remove(floorNumber);
                }
            }
        }
        
        static SimulatorVirus GetVirus()
        {
            SimulatorVirus virus = new SimulatorVirus(.2m, .35m, 5);
            virus.Id = 1;
            virus.TestResultWaitTime = new TimeSpan(3, 0, 0, 0);
            return virus;
        }

        static IList<SimulatorVirusStage> GetVirusStages(int virusId)
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
}