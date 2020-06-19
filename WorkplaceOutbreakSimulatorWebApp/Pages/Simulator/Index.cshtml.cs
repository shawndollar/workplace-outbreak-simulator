using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorEngine.Models;
using WorkplaceOutbreakSimulatorWebApp.Model;
using WorkplaceOutbreakSimulatorWebApp.Services;

namespace WorkplaceOutbreakSimulatorWebApp.Pages.Simulator
{
    public class IndexModel : PageModel
    {

        #region Fields

        private readonly ILogger<IndexModel> _logger;
        private readonly IWebAppService _webAppService;
        private readonly SimulatorEngine _simulatorEngine;
        private readonly IEmployeeDataSource _employeeDataSource;

        #endregion Fields

        #region Properties
        
        [BindProperty]
        public IList<SelectListItem> Employees
        {
            get;
            set;
        } 

        [BindProperty]
        public SimulatorData SimulatorData
        {
            get;
            set;
        } = new SimulatorData();

        [BindProperty]
        public string PageTitle { get => _webAppService.WebApplicationTitle; }

        #endregion Properties

        #region Constructor

        public IndexModel(ILogger<IndexModel> logger, IWebAppService webAppService, IEmployeeDataSource employeeDataSource, SimulatorEngine simulatorEngine)
        {
            _logger = logger;
            _webAppService = webAppService;
            _employeeDataSource = employeeDataSource;
            _simulatorEngine = simulatorEngine;
        }

        #endregion Constructor

        #region Handlers

        public async Task OnGet()
        {
            _simulatorEngine.UpdateConfiguration(SimulatorConfigManager.GetDefaultConfiguration());
            SimulatorData.IsSimulatorRunning = false;
            SimulatorData.IsSimulatorComplete = false;
            SimulatorData.StartDate = _simulatorEngine.Configuration.StartDateTime;
            SimulatorData.EndDate = _simulatorEngine.Configuration.EndDateTime;
            SimulatorData.InfectionRate = Convert.ToInt32((_simulatorEngine.Configuration.Virus.InfectionRate * 100));
            SimulatorData.TestRate = Convert.ToInt32((_simulatorEngine.Configuration.Virus.TestRate * 100));
            TimeSpan ts = _simulatorEngine.Configuration.Virus.TestResultWaitTime;
            SimulatorData.TestResultWaitDays = ts.Days;
            SimulatorData.TestResultWaitHours = ts.Hours;
            SimulatorData.RequiredSickLeaveDays = _simulatorEngine.Configuration.Virus.RecoveryDays;
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (SimulatorData.IsSimulatorComplete)
            {
                SimulatorData.IsSimulatorComplete = true;
                SimulatorData.IsSimulatorRunning = false;
                return await OnDownloadFile();
            }
            else
            {
                SimulatorData.IsSimulatorComplete = false;
                SimulatorData.IsSimulatorRunning = true;

                SimulatorResult simulatorResult = await RunSimulationAsync();
                                
                SimulatorData.IsSimulatorRunning = false;
                SimulatorData.IsSimulatorComplete = true;

                if (simulatorResult.HasError)
                {
                    // Handle error.
                    SimulatorData.IsSimulatorComplete = false;
                }
                else
                {
                    SimulatorData.IsSimulatorComplete = true;
                }

                Employees = GetEmployeeSelectList(_simulatorEngine.Configuration.Employees);

                return Page();
            }
        }
        
        public async Task<IActionResult> OnDownloadFile()
        {
            int selectedEmployeeId = _simulatorEngine.Configuration.Employees.FirstOrDefault(f => f.Id == Int32.Parse(SimulatorData.SelectedEmployeeIdForExport)).Id;
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp");
            string file = "Workplace_Outbreak_Sim_" + _simulatorEngine.Configuration.Employees.FirstOrDefault(f => f.Id == selectedEmployeeId).FullName + ".csv";
            file = FixFileName(file);            

            // Get the data file.
            await WorkplaceOutbreakSimulatorEngine.Helpers.ExportMethods.CreateSimulatorCsvLogAsync(
                _simulatorEngine.EmployeeContacts.Where(f => f.EmployeeId == selectedEmployeeId).ToList(),
                _simulatorEngine.Configuration.Employees,
                _simulatorEngine.Configuration.WorkplaceRooms,
                _simulatorEngine.Configuration.VirusStages,
                Path.Combine(path, file));
            return File(new FileStream(Path.Combine(path, file), FileMode.Open), "application/csv", file);
        }

        #endregion Handlers

        #region Private Methods

        private IList<SelectListItem> GetEmployeeSelectList(IList<SimulatorEmployee> employees)
        {
            var dict = new Dictionary<int, string>(from e in employees
                                                   select new KeyValuePair<int, string>(e.Id, e.FullName)).OrderBy(f => f.Value);
            return (from e in employees
                    select new SelectListItem
                    {
                        Value = e.Id.ToString(),
                        Text = e.FullName
                    }).OrderBy(f => f.Text).ToList();
        }

        private async Task<SimulatorResult> RunSimulationAsync()
        {
            _simulatorEngine.UpdateConfiguration(SimulatorData.StartDate,
               SimulatorData.EndDate,
               Convert.ToDecimal(SimulatorData.InfectionRate) / 100,
               Convert.ToDecimal(SimulatorData.TestRate) / 100,
               new TimeSpan(SimulatorData.TestResultWaitDays.Value, SimulatorData.TestResultWaitHours.Value, 0, 0),
               SimulatorData.RequiredSickLeaveDays,
               _simulatorEngine.Configuration.CanGetSickInOffice);

            IList<SimulatorEmployee> employees;

            if (_simulatorEngine.Configuration.Employees?.Count == 0)
            {
                employees = await RetrieveSimulatorEmployees(_simulatorEngine.Configuration.TotalPeople);
                _simulatorEngine.Configuration.Employees = employees;
            }
            else
            {
                employees = _simulatorEngine.Configuration.Employees;
            }

            // Mark employees who will use break room.
            SimulatorConfigManager.SetEmployeesBreakroomUse(_simulatorEngine.Configuration, .25m);

            // This should be done last before running.
            SimulatorConfigManager.AssignEmployeesToOffices(_simulatorEngine.Configuration);
            
            return await Task<SimulatorResult>.Run(() =>
            {
                return RunSimulation(_simulatorEngine);
            });
        }
        
        private async Task<IList<SimulatorEmployee>> RetrieveSimulatorEmployees(int count)
        {
            // If already have employees, then don't get more.
            if (_simulatorEngine.Configuration.Employees?.Count == count)
            {
                return _simulatorEngine.Configuration.Employees;
            }
            else
            {
                return await GetNewEmployeesAsync(count);
            }
        }

        private async Task<IList<SimulatorEmployee>> GetNewEmployeesAsync(int count)
        {
            string employeesJson = await _employeeDataSource.GetEmployeesAsync(count);            
            var results = JsonSerializer.Deserialize<List<SimulatorEmployee>>(employeesJson);
            return results;
        }
        
        private SimulatorResult RunSimulation(SimulatorEngine simulatorEngine)
        {
            SimulatorResult simulatorResult = new SimulatorResult();
            simulatorEngine.InitializeSimulation();
            do
            {
                var partialSimulatorResult = simulatorEngine.RunNext();
                if (partialSimulatorResult.HasError)
                {
                    simulatorResult.HasError = true;
                    simulatorResult.ErrorMessage = partialSimulatorResult.ErrorMessage;
                    continue;
                }
                foreach (var employee in partialSimulatorResult.EmployeeContacts)
                {
                    simulatorResult.EmployeeContacts.Add(employee);
                }
                simulatorResult.IsSimulatorComplete = partialSimulatorResult.IsSimulatorComplete;
            }
            while (!simulatorResult.IsSimulatorComplete && !simulatorResult.HasError);
            return simulatorResult;
        }

        /// <summary>
        /// Clean up file name to make sure it's valid.
        /// </summary>
        /// <param name="filename">The Faw file name.</param>
        /// <returns>File name without illegal and unwanted characters.</returns>
        private string FixFileName(string filename)
        {
            var invalids = Path.GetInvalidFileNameChars().Concat(new char[] { ' ' });
            return string.Join("_", filename.Split(invalids.ToArray()));
        }

        private SimulatorConfiguration GetDefaultConfigWithExistingEmployees(SimulatorConfiguration configuration)
        {
            SimulatorConfiguration newConfig = SimulatorConfigManager.GetDefaultConfiguration();
            newConfig.Employees = configuration.Employees;
            return newConfig;
        }

        #endregion Private Methods
    }
}