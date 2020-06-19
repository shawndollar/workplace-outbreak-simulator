using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorEngine.Helpers;
using WorkplaceOutbreakSimulatorEngine.Models;

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

            AppSettings appSettings = GetAppSettings();

            LogMessage("INFO", fullCsvOutputFilePath);

            Task runTask = RunSimulationAsync(fullCsvOutputFilePath, appSettings);

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

        static async Task RunSimulationAsync(string csvOutputFile, AppSettings appSettings)
        {
            SimulatorConfiguration simConfig;
            SimulatorEngine simEngine;
            IList<SimulatorEmployeeContact> allEmployeeContacts;
            SimulatorResult simulatorResult;

            try
            {
                simConfig = await CreateConfiguration(appSettings);
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

        static async Task<SimulatorConfiguration> CreateConfiguration(AppSettings appSettings)
        {
            EmployeeDataSource employeeDataSource = new EmployeeDataSource(appSettings.EmployeeApiUri, appSettings.EmployeeApiKey);

            SimulatorConfiguration simConfig = SimulatorConfigManager.GetDefaultConfiguration();

            simConfig.Employees = await GetEmployeesAsync(employeeDataSource, simConfig.FloorPeopleMapping.Sum(f => f.Value));

            // Mark employees who will use break room.
            SimulatorConfigManager.SetEmployeesBreakroomUse(simConfig, .25m);

            // This should always be done last!
            SimulatorConfigManager.AssignEmployeesToOffices(simConfig);

            return simConfig;
        }

        /// <summary>
        /// Get our random employees from employee source.
        /// </summary>
        /// <param name="simDataStore">The employee data source.</param>
        /// <param name="count">The number of employees to get.</param>
        /// <returns>List of employees.</returns>
        static async Task<IList<SimulatorEmployee>> GetEmployeesAsync(EmployeeDataSource simDataStore, int count)
        {
            string employeesJson = null;

            try
            {
                employeesJson = await simDataStore.GetEmployeesAsync(530);
            }
            catch (HttpRequestException exc)
            {
                LogMessage("ERROR", "HTTP Request exception. Unable to retrieve a list of employees: " + exc.ToString());
                throw;
            }
            catch (Exception exc)
            {
                LogMessage("ERROR", "Unable to retrieve a list of employees: " + exc.ToString());
                throw;
            }

            var results = JsonSerializer.Deserialize<IList<SimulatorEmployee>>(employeesJson);

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

        static AppSettings GetAppSettings()
        {
            const string file = "WorkplaceOutbreakSimulatorConsole.settings.json";
            const string apps = "AppSettings", eds = "EmployeeDataSource";
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(file);
                var config = builder.Build();
                AppSettings settings = new AppSettings();
                settings.EmployeeApiUri = config.GetSection($"{apps}:{eds}:ApiUri").Value;
                settings.EmployeeApiKey = config.GetSection($"{apps}:{eds}:ApiKey").Value;
                return settings;
            }
            catch (Exception)
            {
                LogMessage("ERROR", $"Unable to find or configure the {file}.");
                throw;
            }
        }

        static void LogMessage(string level, string message)
        {
            Console.WriteLine($"|{DateTime.Now}|{level}|{message}");
        }

    }
}