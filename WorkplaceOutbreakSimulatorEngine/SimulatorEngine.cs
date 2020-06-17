using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine
{
    public class SimulatorEngine
    {

        #region Fields
        
        private DateTime _simulatorDateTime;
        private int _dataIntervalTotalCount;
        private int _dataIntervalDuringWorkDayCount;
        private Random _random;

        #endregion Fields

        #region Constructor(s)

        public SimulatorEngine(SimulatorConfiguration configuration)
        {
            Configuration = configuration;
            _random = new Random();
        }

        #endregion Constructor(s)

        #region Properties

        public SimulatorConfiguration Configuration { get; }

        public DateTime SimulatorDateTime
        {
            get => _simulatorDateTime;
            private set => _simulatorDateTime = value;
        }

        public int DataIntervalTotalCount
        {
            get => _dataIntervalTotalCount;
            private set => _dataIntervalTotalCount = value;
        }

        public int DataIntervalDuringWorkDayCount
        {
            get => _dataIntervalDuringWorkDayCount;
            private set => _dataIntervalDuringWorkDayCount = value;
        }

        #endregion Properties

        #region Methods
        
        /// <summary>
        /// Run the simulator from start to finish using the configuration data.
        /// </summary>
        /// <returns>A simulator result object with status and stats.</returns>
        public SimulatorResult Run()
        {
            var result = new SimulatorResult();

            try
            {
                InitializeSimulation();
                while (!result.HasError && CanContinueSimulation())
                {
                    AdvanceEmployeesVirusStages();
                    HandleTestResults();
                    TestSickEmployees();
                    UpdateEmployeesLocations();
                    SpreadTheVirus();
                    AdvanceSimulatorDateTime();
                }
            }
            catch (Exception exc)
            {
                result.HasError = true;
                result.ErrorMessage = exc.ToString();
            }

            return result;
        }

        /// <summary>
        /// Loop through all contagious employees who are in the office and spread the virus to the people 
        /// who they are in contact with based on the infection rate.
        /// </summary>
        public void SpreadTheVirus()
        {
            foreach (var infectedEmployee in Configuration.Employees.Where(f => !f.IsOutSick && IsEmployeeContagious(f)))
            {
                var contactedEmployees = Configuration.Employees.Where(f => !f.IsOutSick && f.CurrentRoomId == infectedEmployee.CurrentRoomId && IsEmployeeWell(f));
                foreach (var contactedEmployee in contactedEmployees)
                {
                    if (DoSpreadVirus())
                    {
                        IncrementEmployeeVirusStage(contactedEmployee);                        
                    }
                }
            }
        }

        /// <summary>
        /// Initialize the necessary simulation property values.
        /// </summary>
        private void InitializeSimulation()
        {
            DataIntervalDuringWorkDayCount = 0;
            DataIntervalTotalCount = 0;
            SimulatorDateTime = Configuration.StartDateTime;
        }

        /// <summary>
        /// This advances each employee's virus stage if the employee's time in the current virus stage has elapsed,
        /// and it sets the next scheduled datetime for the virus status to change.
        /// It does not matter if the current time is during work hours.
        /// </summary>
        private void AdvanceEmployeesVirusStages()
        {
            // Update all employees who have a scheduled virus status change at this time.
            foreach (var employee in Configuration.Employees.Where(f => f.ScheduledVirusStageChangeDateTime == SimulatorDateTime))
            {            
                IncrementEmployeeVirusStage(employee);
                var employeeVirusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
                employee.ScheduledVirusStageChangeDateTime = GetNextScheduledVirusStageChangeTime(employeeVirusStage);
            }
        }

        /// <summary>
        /// This will check for any new test results, and will update each employee appropriately.        
        /// </summary>
        private void HandleTestResults()
        {
            // Check for all pending test results and see if the wait time has elapsed.
            foreach (var employee in Configuration.Employees.Where(f => f.InfectionTestDateTime != null && f.InfectiontTestResult == SimulatorDataConstant.InfectionTestResult_Pending))
            {
                if (employee.InfectionTestDateTime.Value.Add(Configuration.Virus.TestResultWaitTime) <= SimulatorDateTime)
                {
                    // For now, we must assume that all Test Results are positive.
                    employee.InfectiontTestResult = SimulatorDataConstant.InfectionTestResult_Positive;
                    employee.SickLeaveStartDateTime = SimulatorDateTime;
                    employee.CurrentRoomId = null;
                }
            }
        }

        /// <summary>
        /// Get sick employees and test them if they get lucky enough to be tested (based on virus test rate).
        /// </summary>
        private void TestSickEmployees()
        {
            foreach (var employee in Configuration.Employees.Where(f => !f.IsOutSick && IsEmployeeSick(f)))
            {
                if (DoTestEmployee())
                {
                    employee.InfectionTestDateTime = SimulatorDateTime;
                    employee.InfectiontTestResult = SimulatorDataConstant.InfectionTestResult_Pending;
                }
            }
        }

        /// <summary>
        /// Update each employees current location based on time of day.
        /// </summary>
        private void UpdateEmployeesLocations()
        {
            if (!IsWorkTime())
            {
                // clear out all current rooms
                foreach (var employee in Configuration.Employees)
                {
                    employee.CurrentRoomId = null;
                }
                // No need to do anything more during non-business hours.
                return;
            }

            // Bring back everyone who is out sick and whose recovery time has elapsed.
            foreach (var employee in Configuration.Employees.Where(f => f.IsOutSick))
            {
                if (employee.SickLeaveStartDateTime.Value.AddDays(Configuration.Virus.RecoveryDays) <= SimulatorDateTime)
                {
                    // This marks the employee as back at work.
                    employee.SickLeaveStartDateTime = null;
                }
            }

            bool isBreakTime = IsBreakTime();


            // Move all of the people who are not out sick to their respective offices or the break room.
            foreach (var employee in Configuration.Employees.Where(f => !f.IsOutSick))
            {
                if (isBreakTime && employee.IsBreakroomUser)
                {
                    MoveEmployeeToBreakroom(employee);
                }
                else
                {
                    employee.CurrentRoomId = employee.OfficeId;
                }
            }

            // Lastly, move everyone to their meetings if it's not break time.
            if (!isBreakTime)
            {
                // we'll fill up all of the meeting rooms, so we'll loop through every meeting room
                foreach (var meetingRoom in Configuration.WorkplaceRooms.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Meeting))
                {
                    MoveEmployeesToMeeting(meetingRoom);
                }
            }
        }
                
        /// <summary>
        /// Move random employees to meeting locations.
        /// </summary>
        /// <param name="meetingRoom">The meeting room to move employees into.</param>
        private void MoveEmployeesToMeeting(SimulatorWorkplaceRoom meetingRoom)
        {            
            int minAttendees = Configuration.MinMeetingAttendance;
            int maxAttendees = Configuration.MaxMeetingAttendance;
            Random random = new Random();
            int meetingAttendanceSize = random.Next(minAttendees, maxAttendees + 1);

            // Get all nonemployees who are in their offices.
            int[] employeesInOffice = (from e in Configuration.Employees
                                     where !e.IsOutSick && e.CurrentRoomId == e.OfficeId
                                     select e.Id).ToArray();

            if (employeesInOffice.Length < minAttendees)
            {
                // if not enough people to fill a meeting, then quit now
                return;
            }

            IList<int> meetingAttendees = new List<int>();

            // Grab random employees and add them to meeting attendee list.
            // There's danger of a slow loop here, but it's highly unlikely.
            while (meetingAttendees.Count < meetingAttendanceSize)
            {
                int id = random.Next(employeesInOffice.Length);
                if (!meetingAttendees.Contains(id))
                {
                    meetingAttendees.Add(id);
                }
            }

            // Finally, update our meeting employees' current room IDs.
            foreach (var employeeId in meetingAttendees)
            {
                var employee = Configuration.Employees.First(f => f.Id == employeeId);
                employee.CurrentRoomId = meetingRoom.Id;
            }

        }

        /// <summary>
        /// Move employee to a breakroom.
        /// </summary>
        /// <param name="employee">The employee to move. The CurrentRoomId will be updated.</param>
        private void MoveEmployeeToBreakroom(SimulatorEmployee employee)
        {
            int floorId = Configuration.WorkplaceRooms.First(f => f.Id == employee.OfficeId).FloorId;
            IList<SimulatorWorkplaceRoom> availableBreakRooms = Configuration.WorkplaceRooms.Where(f => f.FloorId == floorId && f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Breakroom).ToList();
            if (availableBreakRooms.Count == 1)
            {
                // If only one break room on this floor, then put them in there.
                employee.CurrentRoomId = availableBreakRooms[0].Id;
            }
            else
            {
                // If more than one break room, then pick random one.
                Random random = new Random();
                employee.CurrentRoomId = availableBreakRooms[random.Next(0, availableBreakRooms.Count)].Id;
            }
        }

        /// <summary>
        /// Determine whether simulation can continue.
        /// </summary>
        /// <returns>True if can continue. False if end of simulation has been reached.</returns>
        private bool CanContinueSimulation()
        {
            return SimulatorDateTime <= Configuration.EndDateTime;
        }

        /// <summary>
        /// Move the simulator datetime ahead by the configured time interval.
        /// </summary>
        private void AdvanceSimulatorDateTime()
        {
            SimulatorDateTime = SimulatorDateTime.Add(Configuration.DataInterval);
        }

        /// <summary>
        /// Determine if the employee is sick based on his virus stage and the virus stage sickness property.
        /// </summary>
        /// <param name="employee">The employee to check.</param>
        /// <returns>Return if the employee is in a stage that is counted as "sick".</returns>
        private bool IsEmployeeSick(SimulatorEmployee employee)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
            return virusStage != null && virusStage.IsSick;
        }

        /// <summary>
        /// Determine if the employee is contagious based on his virus stage and the virus stage contagious property.
        /// </summary>
        /// <param name="employee">The employee to check.</param>
        /// <returns>Return if the employee is in a stage that is counted as contagious.</returns>
        private bool IsEmployeeContagious(SimulatorEmployee employee)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
            return virusStage != null && virusStage.IsContagious;
        }

        /// <summary>
        /// Determine if the employee is infected based on his virus stage and the virus stage infected property.
        /// </summary>
        /// <param name="employee">The employee to check.</param>
        /// <returns>Return if the employee is infected.</returns>
        private bool IsEmployeeInfected(SimulatorEmployee employee)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
            return virusStage != null && virusStage.IsInfected;
        }

        /// <summary>
        /// Determine if the employee is immune based on his virus stage and the virus stage property.
        /// </summary>
        /// <param name="employee">The employee to check.</param>
        /// <returns>Return if the employee is immune.</returns>
        private bool IsEmployeeImmune(SimulatorEmployee employee)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
            return virusStage != null && virusStage.InfectionStage == SimulatorDataConstant.InfectionStage_Immune;
        }

        /// <summary>
        /// Determine if the employee is "well" based on his virus stage and the virus stage property.
        /// </summary>
        /// <param name="employee">The employee to check.</param>
        /// <returns>Return if the employee is "well".</returns>
        private bool IsEmployeeWell(SimulatorEmployee employee)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
            return virusStage != null && virusStage.InfectionStage == SimulatorDataConstant.InfectionStage_Well;
        }

        /// <summary>
        /// Determine whether to issue a test based on test rate.
        /// </summary>
        /// <returns>Return true if a test should be issued.</returns>
        private bool DoTestEmployee()
        {
            // Pick a random number between 1 and 1000 and see if it's within our test rate. 
            // Example: There is a 25% (.25*1000) chance that the number will be between 1 and 250.
            // Multiply by 1000 instead of 100 just in case test rate is has thousandths place (which is very likely). It'll be more exact.
            int randomNumber = _random.Next(1, 1001);
            return randomNumber <= Convert.ToInt32(Configuration.Virus.TestRate * 1000);
        }

        /// <summary>
        /// Given a certain infection rate, this will determine if the infection was spread.
        /// We're just using the infection rate and checking to see if a random number falls within that number.
        /// This is a little different than saying that the virus infects x% of the people in contact.
        /// </summary>
        /// <returns>True if the virus should be spread.</returns>
        private bool DoSpreadVirus()
        {
            // Pick a random number between 1 and 1000 and see if it's within our infection rate. 
            // Example: There is a 25% (.25*1000) chance that the number will be between 1 and 250.
            // Multiply by 1000 instead of 100 just in case rate is has thousandths place (which is very likely). It'll be more exact.
            int randomNumber = _random.Next(1, 1001);
            return randomNumber <= Convert.ToInt32(Configuration.Virus.InfectionRate * 1000);
        }

        /// <summary>
        /// Determine if the current simulator time is during the working day/hours.
        /// </summary>
        /// <returns>True if the date/time is during the work day hours.</returns>
        private bool IsWorkTime()
        {
            // No work on the weekends.
            if (SimulatorDateTime.DayOfWeek == DayOfWeek.Saturday ||
                SimulatorDateTime.DayOfWeek == DayOfWeek.Sunday)
            {
                return false;
            }

            // No work outside of the workday hours.
            if (SimulatorDateTime.TimeOfDay < Configuration.StartOfWorkday || SimulatorDateTime.TimeOfDay > Configuration.EndOfWorkday)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check to see if it's break time.
        /// </summary>
        /// <returns>Return true if time of day is between the break time hours.</returns>
        private bool IsBreakTime()
        {
            TimeSpan breakTime = Configuration.BreakTimeOfDay;
            return SimulatorDateTime.TimeOfDay >= breakTime && SimulatorDateTime.TimeOfDay < breakTime.Add(Configuration.BreakTimeSpan);
        }

        /// <summary>
        /// Move the virus stage to the next level in the virus sequence.
        /// </summary>
        /// <param name="employee">The employee to update.</param>
        private void IncrementEmployeeVirusStage(SimulatorEmployee employee)
        {
            var currentStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
            int nextStageOrder = currentStage.StageOrder + 1;
            var newStage = Configuration.VirusStages.FirstOrDefault(f => f.VirusId == currentStage.VirusId && f.StageOrder == nextStageOrder);
            // There should never be a case where the next stage does not exist.
            // But if it happens, just leave the stage where it is.
            if (newStage != null)
            {
                employee.VirusStageId = newStage.Id;
                employee.VirusStageLastChangeDateTime = SimulatorDateTime;
            }
        }

        /// <summary>
        /// Determine how long this virus stage should last based on the virus stage configuration.
        /// </summary>
        /// <param name="virusStage">The virus stage to determine the length of.</param>
        /// <returns>The datetime when the virus stage should be updated next.</returns>
        private DateTime? GetNextScheduledVirusStageChangeTime(SimulatorVirusStage virusStage)
        {
            // If this stage has no min and max, then return null.
            if (virusStage.MinDays == 0 && virusStage.MaxDays == 0)
            {
                return null;
            }

            // If min is same as max, then it's a fixed time and we just add the days to the current datetime.
            if (virusStage.MinDays == virusStage.MaxDays)
            {
                return SimulatorDateTime.AddDays(virusStage.MinDays);
            }

            // If it's a range, then we need to get a random number between the min and max.
            Random random = new Random();
            var randomDays = random.Next(virusStage.MinDays, virusStage.MaxDays + 1);
            return SimulatorDateTime.AddDays(randomDays);
        }

        #endregion Methods
    }
}