using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorWebApp.Services;
using System.ComponentModel.DataAnnotations;
using WorkplaceOutbreakSimulatorWebApp.Model;

namespace WorkplaceOutbreakSimulatorWebApp.Pages
{
    public class IndexModel : PageModel
    {
        #region Fields
        
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebAppService _webAppService;

        #endregion Fields

        #region Properties
        
        [BindProperty]
        public ConfigData ConfigData { get; set; }               

        [BindProperty]
        public string PageTitle { get => _webAppService.WebApplicationTitle; }

        #endregion Properties

        #region Constructor

        public IndexModel(ILogger<IndexModel> logger, IWebAppService webAppService)
        {
            _logger = logger;
            _webAppService = webAppService;
        }

        #endregion Constructor

        #region Handlers

        public async Task OnGet()
        {
            var defaultConfig = SimulatorConfigManager.GetDefaultConfiguration();
            ConfigData = new ConfigData();
            ConfigData.StartDate = defaultConfig.StartDateTime;
            ConfigData.EndDate = defaultConfig.EndDateTime;
            ConfigData.InfectionRate = Convert.ToInt32((defaultConfig.Virus.InfectionRate * 100));
            ConfigData.TestRate = Convert.ToInt32((defaultConfig.Virus.TestRate * 100));
            TimeSpan ts = defaultConfig.Virus.TestResultWaitTime;
            ConfigData.TestResultWaitDays = ts.Days;
            ConfigData.TestResultWaitHours = ts.Hours;
            await Task.CompletedTask;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            
            await Task.CompletedTask;

            return Page();
        }

        #endregion Handlers
    }
}
