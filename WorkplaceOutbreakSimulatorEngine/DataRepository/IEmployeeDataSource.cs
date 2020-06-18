using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WorkplaceOutbreakSimulatorEngine.DataRepository
{
    public interface IEmployeeDataSource
    {
        string DataApiKey { get; }
        string DataApiEndpoint { get; }
        Task<string> GetEmployeesAsync(int count);
        Task<string> GetEmployeesFromFileAsync(string location);
        string GetApiUrl(int count);
    }
}
