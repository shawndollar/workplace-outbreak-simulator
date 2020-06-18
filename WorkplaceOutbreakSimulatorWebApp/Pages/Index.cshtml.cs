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

        private readonly IWebAppService _webAppService;

        [BindProperty]
        public string PageTitle { get => _webAppService.WebApplicationTitle; }

        #region Handlers

        public IndexModel(IWebAppService webAppService)
        {
            _webAppService = webAppService;
        }

        public async Task<IActionResult> OnGet()
        {
            await Task.CompletedTask;
            return RedirectToPage(_webAppService?.SimulatorPage);
        }

        #endregion Handlers
    }
}