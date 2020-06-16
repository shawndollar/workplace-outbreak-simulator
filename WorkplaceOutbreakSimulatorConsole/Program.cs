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

namespace WorkplaceOutbreakSimulatorConsole
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            SimulatorConfiguration simConfig = new SimulatorConfiguration();

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


            //try
            //{
            //    IList<SimulatorEmployee> employees = await GetEmployees(530);
            //    employees.ToList().ForEach(f => Console.WriteLine(f.ToString()));
            //}
            //catch (Exception exc)
            //{
            //    Console.WriteLine("Error: " + exc.Message);

            //}

            SimulatorEngine simEngine = new SimulatorEngine(simConfig);

            await Task.CompletedTask;

            Console.ReadKey();

        }

        static SimulatorConfiguration CreateConfiguration()
        {
            int numberOfFloors = 5;

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
            simConfig.WorkplaceFloors = GetWorkplaceFloors(simConfig.Workplace.Id, 5);

            return simConfig;

        }

        static SimulatorWorkplace GetWorkplace()
        {
            var wp = new SimulatorWorkplace();
            wp.Id = 1;
            wp.Name = "Default";
            return wp;
        }

        static IList<SimulatorWorkplaceRoom> GetWorkplaceRooms(SimulatorWorkplaceFloor floors, int numberOfRooms)
        {
            IList<SimulatorWorkplaceRoom> rooms = new List<SimulatorWorkplaceRoom>();

            return rooms;
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

        static async Task<IList<SimulatorEmployee>> GetEmployees(int count)
        {
            SimulatorDataStore simDataStore = new SimulatorDataStore("https://api.mockaroo.com/api/f028dfc0", "89c948e0");
            string employeesJson = await simDataStore.GetEmployees(530);
            var results = JsonSerializer.Deserialize<IList<SimulatorEmployee>>(employeesJson);
            return results;
        }

    }
}