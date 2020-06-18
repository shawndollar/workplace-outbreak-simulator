using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkplaceOutbreakSimulatorWebApp.Services
{
    public interface IWebAppService
    {
        public string WebApplicationTitle { get; }

        public string SimulatorPage { get; }

    }
}