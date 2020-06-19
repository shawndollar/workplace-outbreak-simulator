using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
            IList<string> lines = new List<string>(contacts.Count);

            int maxContacts = 0;

            string header = "ContactDateTime,EmployeeId,Name,RoomId,RoomType,VirusStatus";

            foreach (var contact in contacts)
            {
                StringBuilder sb = new StringBuilder("");
                string employeeFullName = employees.FirstOrDefault(f => f.Id == contact.EmployeeId)?.FullName;
                var virusStage = virusStages.FirstOrDefault(f => f.Id == contact.VirusStageId);
                string roomType = (rooms.FirstOrDefault(f => f.Id == contact.RoomId)?.RoomType) ?? "None";

                sb.Append(contact.ContactDateTime);
                sb.Append(",");
                sb.Append(contact.EmployeeId);
                sb.Append(",");
                sb.Append(employeeFullName);
                sb.Append(",");
                sb.Append(contact.RoomId);
                sb.Append(",");
                sb.Append(roomType);
                sb.Append(",");
                sb.Append(virusStage?.InfectionStage);

                {
                    int contactCount = 0;
                    for (int i = 0; i < contact.EmployeeContacts?.Count; i++)
                    {
                        contactCount++;
                        sb.Append(",");
                        var contactEmployee = employees.FirstOrDefault(f => f.Id == contact.EmployeeContacts[i].EmployeeId);
                        sb.Append(contactEmployee.FullName);
                    }
                    if (contactCount > maxContacts)
                    {
                        maxContacts = contactCount;
                    }
                }

                lines.Add(sb.ToString());
            }

            for (int i = 0; i < maxContacts; i++)
            {
                header += $",Contact{i}";
            }

            using (var writer = new StreamWriter(outputFile))
            {
                await writer.WriteLineAsync(header);
                foreach (var line in lines)
                {
                    await writer.WriteLineAsync(line);
                }
            }
        }
    }
}