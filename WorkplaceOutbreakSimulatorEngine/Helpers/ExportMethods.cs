using CsvHelper;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine.Helpers
{
    public static class ExportMethods
    {
        /// <summary>
        /// Create and write the employee contact info to a CSV file.
        /// </summary>
        /// <param name="contacts">The records to write out to the CSV file.</param>
        /// <param name="employees">The full list of employees for looking up stuff.</param>
        /// <param name="rooms">The full list of rooms for looking up room info.</param>
        /// <param name="virusStages">The full list of virus stages for looking up virus stage info.</param>
        /// <param name="outputFile">The output file to write to.</param>
        /// <returns>Task with no return value.</returns>
        public static async Task CreateSimulatorCsvLogAsync(IList<SimulatorEmployeeContact> contacts,
            IList<SimulatorEmployee> employees,
            IList<SimulatorWorkplaceRoom> rooms,
            IList<SimulatorVirusStage> virusStages,
            string outputFile)
        {
            var dynamicList = new List<dynamic>(contacts.Count);

            foreach (var contact in contacts)
            {
                var employee = employees.FirstOrDefault(f => f.Id == contact.EmployeeId);
                var virusStage = virusStages.FirstOrDefault(f => f.Id == contact.VirusStageId);
                var workplaceRoom = rooms.FirstOrDefault(f => f.Id == contact.RoomId);
                dynamic d = new ExpandoObject();
                d.ContactDateTime = contact.ContactDateTime;
                d.EmployeeId = employee?.Id;
                d.Name = employee?.FullName;
                // d.Gender = employee?.Gender;
                d.RoomId = contact.RoomId;
                d.RoomType = (workplaceRoom == null) ? "None" : workplaceRoom.RoomType;
                d.VirusStatus = virusStage?.InfectionStage;
                for (int i = 0; i < contact.EmployeeContacts?.Count; i++)
                {
                    var contactEmployee = employees.FirstOrDefault(f => f.Id == contact.EmployeeContacts[i].EmployeeId);
                    AddDynamicProperty(d, $"Contact{i + 1}", contactEmployee.FullName);
                }
                dynamicList.Add(d);
            }

            using (var writer = new StreamWriter(outputFile))
            {
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    await csv.WriteRecordsAsync(dynamicList);
                }
            }
        }

        /// <summary>
        /// Use IDictionary methods of ExpandoObject to add properties.
        /// </summary>
        /// <param name="expando">The ExpandObject class to add a property</param>
        /// <param name="propertyName">The name of the new property.</param>
        /// <param name="propertyValue">The object of the new property.</param>
        public static void AddDynamicProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
            {
                expandoDict[propertyName] = propertyValue;
            }
            else
            {
                expandoDict.Add(propertyName, propertyValue);
            }
        }

    }
}