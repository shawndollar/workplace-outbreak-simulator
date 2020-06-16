using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine.DataRepository
{
    public class SimulatorDataStore
    {

        public string DataApiKey { get; }

        public string DataApiEndpoint { get;  }

        public SimulatorDataStore(string apiEndpoint, string dataApiKey)
        {
            DataApiEndpoint = apiEndpoint;
            DataApiKey = dataApiKey;
        }

        public async Task<string> GetEmployees(int count)
        {

            using (HttpClient client = new HttpClient())
            {
                string uri = GetApiUrl(count);
                var response = await client.GetStringAsync(uri);
                return response;
            }

        }

        private string GetApiUrl(int count)
        {
            return $"{DataApiEndpoint}?count={count}&key={DataApiKey}";
        }

    }
}