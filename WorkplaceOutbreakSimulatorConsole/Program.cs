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
using System.Threading;

namespace WorkplaceOutbreakSimulatorConsole
{
    class Program
    {
        public static async Task<int> Main(string[] args)
        {
            LogMessage("INFO", "Starting Program");

            if (!IsInputValid(args))
            {
                return -1;
            }

            string outputFileLocation = args[0];
            string csvOutputFile = GetOutputFileName();

            if (!IsFileOutputFolderValid(outputFileLocation, csvOutputFile))
            {
                return -2;
            }

            string fullCsvOutputFilePath = Path.GetFullPath(Path.Combine(outputFileLocation, csvOutputFile));

            LogMessage("INFO", fullCsvOutputFilePath);

            Task runTask = RunSimulationAsync(fullCsvOutputFilePath);

            try
            {
                await runTask;
                LogMessage("INFO", "Finished Program");
            }
            catch (Exception exc)
            {
                LogMessage("INFO", $"Simulation task status is '{runTask.Status}'. Exception message: {exc.Message}");
                return -1;
            }

            return 0;
        }

        static async Task RunSimulationAsync(string csvOutputFile)
        {
            SimulatorConfiguration simConfig;
            SimulatorEngine simEngine;
            IList<SimulatorEmployeeContact> allEmployeeContacts;
            SimulatorResult simulatorResult;

            try
            {
                simConfig = await CreateConfiguration(false);
                simEngine = new SimulatorEngine(simConfig);
                allEmployeeContacts = new List<SimulatorEmployeeContact>();
                simulatorResult = new SimulatorResult();
                simEngine.InitializeSimulation();
            }
            catch (Exception exc)
            {
                LogMessage("ERROR", "Unable to configure and initialize the simulation: " + exc.ToString());
                throw exc;
            }

            try
            {
                LogMessage("INFO", "Starting simulation.");
                do
                {
                    simulatorResult = simEngine.RunNext();
                    allEmployeeContacts.AddRange(simulatorResult.EmployeeContacts);
                }
                while (!simulatorResult.IsSimulatorComplete && !simulatorResult.HasError);
                LogMessage("DEBUG", "Simulation complete. Creating output CSV file.");
                await ExportMethods.CreateSimulatorCsvLogAsync(allEmployeeContacts, simConfig.Employees, simConfig.WorkplaceRooms, simConfig.VirusStages, csvOutputFile);
            }
            catch (Exception exc)
            {
                LogMessage("ERROR", "Unable to complete simulation: " + exc.ToString());
                throw exc;
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
                    LogMessage("ERROR", "Unable to retrieve a list of employees: " + exc.ToString());
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

        static bool IsInputValid(string[] args)
        {
            if (args == null || args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
            {
                LogMessage("ERROR", "Invalid input args. First arg must be the output file location.");
                return false;
            }

            return true;
        }

        static string GetOutputFileName()
        {
            return "Workplace_Outbreak_Simulator_" + System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + ".csv";
        }

        /// <summary>
        /// Make user output location is okay and can write to it.
        /// </summary>
        /// <param name="path">Output Folder</param>
        /// <param name="fileName">Output File Name</param>
        /// <returns>true if path exists and we can write to it.</returns>
        static bool IsFileOutputFolderValid(string path, string fileName)
        {
            if (!Directory.Exists(path))
            {
                LogMessage("ERROR", $"Folder {path} does not exist. Create the folder or select a new output folder.");
                return false;
            }

            string output = Path.Combine(path, fileName);

            try
            {
                using (FileStream fs = new FileStream(output, FileMode.Create, FileAccess.Write))
                {
                    fs.WriteByte(0x00);
                }
            }
            catch (Exception exc)
            {
                LogMessage("ERROR", $"Unable to write a file to {path}. Change permissions or select a new output folder: " + exc.Message);
                return false;
            }

            // Should be no problem deleting this.
            File.Delete(output);

            return true;
        }

        static void LogMessage(string level, string message)
        {
            Console.WriteLine($"|{DateTime.Now}|{level}|{message}");
        }

    }
}