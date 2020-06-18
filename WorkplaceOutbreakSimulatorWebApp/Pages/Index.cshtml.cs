using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorWebApp.Model;
using WorkplaceOutbreakSimulatorWebApp.Services;

namespace WorkplaceOutbreakSimulatorWebApp.Pages
{
    public class IndexModel : PageModel
    {
        //#region Fields

        //private readonly ILogger<IndexModel> _logger;
        //private readonly IWebAppService _webAppService;
        //private readonly SimulatorEngine _simulatorEngine;

        //#endregion Fields

        //#region Properties

        //[BindProperty]
        //public ConfigData ConfigData { get; set; }               

        //[BindProperty]
        //public string PageTitle { get => _webAppService.WebApplicationTitle; }

        //#endregion Properties

        //#region Constructor

        //public IndexModel(ILogger<IndexModel> logger, IWebAppService webAppService, SimulatorEngine simulatorEngine)
        //{
        //    _logger = logger;
        //    _webAppService = webAppService;
        //    _simulatorEngine = simulatorEngine;
        //}

        //#endregion Constructor

        #region Handlers

        public async Task<IActionResult> OnGet()
        {
            await Task.CompletedTask;
            return RedirectToPage("Simulator/Index");
        }

        //public async Task<IActionResult> OnPost()
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return Page();
        //    }

        //    _simulatorEngine.UpdateConfiguration(ConfigData.StartDate, 
        //        ConfigData.EndDate, 
        //        Convert.ToDecimal(ConfigData.InfectionRate)/100, 
        //        Convert.ToDecimal(ConfigData.TestRate)/100, 
        //        new TimeSpan(ConfigData.TestResultWaitDays.Value, ConfigData.TestResultWaitHours.Value, 0, 0), 
        //        ConfigData.RequiredSickLeaveDays);

        //    await Task.CompletedTask;

        //    return Page();
        //}

        #endregion Handlers
    }
}
