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

                    sw.WriteLine(simEngine.SimulatorDateTime + " Infected: " + (from e in simConfig.Employees
                                                                                join v in simConfig.VirusStages
                                                                                on e.VirusStageId equals v.Id
                                                                                where v.IsInfected
                                                                                select e).Count() + " Immune: " +
                                                                                (from e in simConfig.Employees
                                                                                 join v in simConfig.VirusStages
                                                                                 on e.VirusStageId equals v.Id
                                                                                 where v.InfectionStage ==
                                                                                 SimulatorDataConstant.InfectionStage_Immune
                                                                                 select e).Count());
                    allEmployeeContacts.AddRange(simulatorResult.EmployeeContacts);
                }
                while (!simulatorResult.IsSimulatorComplete && !simulatorResult.HasError);

                sw.WriteLine("Status " + DateTime.Now + " " + !simulatorResult.HasError + " " + simulatorResult.ErrorMessage);
            }
            finally
            {
                sw.Close();
            }

            await CreateSimulatorCsvLogAsync(allEmployeeContacts, simConfig.Employees, simConfig.WorkplaceRooms, simConfig.VirusStages, csvOutputFile);
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
            EmployeeDataSource employeeDataSource = new EmployeeDataSource("https://api.mockaroo.com/api/f028dfc0", "89c948e0");
            
            SimulatorConfiguration simConfig = SimulatorConfigManager.GetDefaultConfiguration();
            
            simConfig.Employees = await GetEmployeesAsync(employeeDataSource, simConfig.FloorPeopleMapping.Sum(f => f.Value), useTestPersonFile);

            // Mark employees who will use break room.
            SimulatorConfigManager.SetEmployeesBreakroomUse(simConfig, .25m);

            // This should always be done last!
            SimulatorConfigManager.AssignEmployeesToOffices(simConfig);

            return simConfig;
        }

       
        static async Task<IList<SimulatorEmployee>> GetEmployeesAsync(EmployeeDataSource simDataStore, int count, bool getFromFile)
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