﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkplaceOutbreakSimulatorEngine;
using WorkplaceOutbreakSimulatorWebApp.Model;
using WorkplaceOutbreakSimulatorWebApp.Services;

namespace WorkplaceOutbreakSimulatorWebApp.Pages.Simulator
{
    public class _SimulatorOutputModel : PageModel
    {

        #region Fields

        private readonly IWebAppService _webAppService;
        private readonly SimulatorEngine _simulatorEngine;

        #endregion Fields

        #region Properties

        [BindProperty]
        public SimulatorOutput SimulatorOutput { get; set; }

        #endregion Properties

        #region Constructor

        public _SimulatorOutputModel(IWebAppService webAppService, SimulatorEngine simulatorEngine)
        {
            _webAppService = webAppService;
            _simulatorEngine = simulatorEngine;
        }

        #endregion Constructor

        public void OnGet()
        {

        }
    }
}