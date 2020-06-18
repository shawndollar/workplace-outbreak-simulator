using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        public SimulatorData SimulatorVM
        {
            get;
            set;
        } = new SimulatorData() { SimulatorInput = new SimulatorInput(), SimulatorOutput = new SimulatorOutput() };

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
            SimulatorVM.IsSimulatorRunning = false;
            SimulatorVM.IsSimulatorComplete = false;
            SimulatorVM.SimulatorInput.StartDate = _simulatorEngine.Configuration.StartDateTime;
            SimulatorVM.SimulatorInput.EndDate = _simulatorEngine.Configuration.EndDateTime;
            SimulatorVM.SimulatorInput.InfectionRate = Convert.ToInt32((_simulatorEngine.Configuration.Virus.InfectionRate * 100));
            SimulatorVM.SimulatorInput.TestRate = Convert.ToInt32((_simulatorEngine.Configuration.Virus.TestRate * 100));
            TimeSpan ts = _simulatorEngine.Configuration.Virus.TestResultWaitTime;
            SimulatorVM.SimulatorInput.TestResultWaitDays = ts.Days;
            SimulatorVM.SimulatorInput.TestResultWaitHours = ts.Hours;
            SimulatorVM.SimulatorInput.RequiredSickLeaveDays = _simulatorEngine.Configuration.Virus.RecoveryDays;            
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            SimulatorVM.IsSimulatorComplete = false;
            SimulatorVM.IsSimulatorRunning = true;
            
            await RunSimulationAsync();

            SimulatorVM.IsSimulatorRunning = false;
            SimulatorVM.IsSimulatorComplete = true;

            return Page();
        }

        #endregion Handlers

        #region Private Methods

        private async Task<SimulatorResult> RunSimulationAsync()
        {
            _simulatorEngine.UpdateConfiguration(SimulatorVM.SimulatorInput.StartDate,
               SimulatorVM.SimulatorInput.EndDate,
               Convert.ToDecimal(SimulatorVM.SimulatorInput.InfectionRate) / 100,
               Convert.ToDecimal(SimulatorVM.SimulatorInput.TestRate) / 100,
               new TimeSpan(SimulatorVM.SimulatorInput.TestResultWaitDays.Value, SimulatorVM.SimulatorInput.TestResultWaitHours.Value, 0, 0),
               SimulatorVM.SimulatorInput.RequiredSickLeaveDays);

            var employees = await GetEmployeesAsync(_simulatorEngine.Configuration.TotalPeople, false);

            _simulatorEngine.Configuration.Employees = employees;

            // Mark employees who will use break room.
            SimulatorConfigManager.SetEmployeesBreakroomUse(_simulatorEngine.Configuration, .25m);

            // This should be done last before running.
            SimulatorConfigManager.AssignEmployeesToOffices(_simulatorEngine.Configuration);

            return await Task<SimulatorResult>.Run(() =>
            {
                return RunSimulation(_simulatorEngine);
            });
        }

        private async Task<IList<SimulatorEmployee>> GetEmployeesAsync(int count, bool getFromFile)
        {
            string employeesJson = null;

            if (!getFromFile)
            {
                try
                {
                    employeesJson = await _employeeDataSource.GetEmployeesAsync(count);
                }
                catch (Exception exc)
                {
                    Console.WriteLine(exc);
                }
            }

            if (string.IsNullOrWhiteSpace(employeesJson))
            {
                employeesJson = await _employeeDataSource.GetEmployeesFromFileAsync(@"C:\projects\repos\workplace-outbreak-simulator-repo\employees.json");
            }

            var results = JsonSerializer.Deserialize<IList<SimulatorEmployee>>(employeesJson);

            if (results.Count > count)
            {
                results = results.Take(count).ToList();
            }

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

        #endregion Private Methods
    }
}