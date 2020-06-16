using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine
{
    public class SimulatorEngine
    {
        public SimulatorConfiguration Configuration { get; }

        public SimulatorEngine(SimulatorConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public SimulatorResult Run()
        {
            throw new NotImplementedException();
        }
    }
}