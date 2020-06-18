using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
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

        #endregion Fields

        #region Properties

        [BindProperty]
        public ConfigData ConfigData { get; set; }

        [BindProperty]
        public string PageTitle { get => _webAppService.WebApplicationTitle; }

        #endregion Properties

        #region Constructor

        public IndexModel(ILogger<IndexModel> logger, IWebAppService webAppService, SimulatorEngine simulatorEngine)
        {
            _logger = logger;
            _webAppService = webAppService;
            _simulatorEngine = simulatorEngine;
        }

        #endregion Constructor

        #region Handlers

        public async Task OnGet()
        {
            _simulatorEngine.UpdateConfiguration(SimulatorConfigManager.GetDefaultConfiguration());
            ConfigData = new ConfigData();
            ConfigData.StartDate = _simulatorEngine.Configuration.StartDateTime;
            ConfigData.EndDate = _simulatorEngine.Configuration.EndDateTime;
            ConfigData.InfectionRate = Convert.ToInt32((_simulatorEngine.Configuration.Virus.InfectionRate * 100));
            ConfigData.TestRate = Convert.ToInt32((_simulatorEngine.Configuration.Virus.TestRate * 100));
            TimeSpan ts = _simulatorEngine.Configuration.Virus.TestResultWaitTime;
            ConfigData.TestResultWaitDays = ts.Days;
            ConfigData.TestResultWaitHours = ts.Hours;
            ConfigData.RequiredSickLeaveDays = _simulatorEngine.Configuration.Virus.RecoveryDays;
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _simulatorEngine.UpdateConfiguration(ConfigData.StartDate,
                ConfigData.EndDate,
                Convert.ToDecimal(ConfigData.InfectionRate) / 100,
                Convert.ToDecimal(ConfigData.TestRate) / 100,
                new TimeSpan(ConfigData.TestResultWaitDays.Value, ConfigData.TestResultWaitHours.Value, 0, 0),
                ConfigData.RequiredSickLeaveDays);

            await Task.CompletedTask;

            return Page();
        }

        #endregion Handlers
    }
}