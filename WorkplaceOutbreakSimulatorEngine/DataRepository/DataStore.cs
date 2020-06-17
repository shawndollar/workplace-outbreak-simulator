using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine.DataRepository
{
    public class SimulatorDataStore
    {

        private readonly object _randomLock = new object();

        public string DataApiKey { get; }

        public string DataApiEndpoint { get;  }
        
        private Random Randomizer { get; }

        public SimulatorDataStore(string apiEndpoint, string dataApiKey)
        {
            DataApiEndpoint = apiEndpoint;
            DataApiKey = dataApiKey;
            Randomizer = new Random();
        }

        public async Task<string> GetEmployeesAsync(int count)
        {
            using (HttpClient client = new HttpClient())
            {
                string uri = GetApiUrl(count);
                var response = await client.GetStringAsync(uri);
                return response;
            }

        }

        public async Task<string> GetEmployeesFromFileAsync(string location)
        {
            using (StreamReader sr = new StreamReader(location))
            {
                return await sr.ReadToEndAsync();
            }
        }

        public int GetRandomNumber(int min, int max)
        {
            int number;
            lock (_randomLock)
            {
                number = Randomizer.Next(min, max);
            }
            return number;
        }

        private string GetApiUrl(int count)
        {
            return $"{DataApiEndpoint}?count={count}&key={DataApiKey}";
        }

    }
}