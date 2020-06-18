using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkplaceOutbreakSimulatorWebApp.Services
{
    public class WebAppService : IWebAppService
    {
        public string WebApplicationTitle { get => "Workplace Outbreak Simulator"; }

        public string SimulatorPage { get => "Simulator/Index"; }
    }
}