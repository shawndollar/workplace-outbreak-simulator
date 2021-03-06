﻿using System;
using System.Collections.Generic;
using System.Linq;
using WorkplaceOutbreakSimulatorEngine.DataRepository;
using WorkplaceOutbreakSimulatorEngine.Helpers;
using WorkplaceOutbreakSimulatorEngine.Models;

namespace WorkplaceOutbreakSimulatorEngine
{
    public class SimulatorEngine
    {

        #region Fields

        private readonly object _randomLockObject = new object();
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

        public SimulatorConfiguration Configuration { get; private set; }

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

        public IList<SimulatorEmployeeContact> EmployeeContacts { get; private set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Initialize the necessary simulation property values.
        /// </summary>
        public void InitializeSimulation()
        {
            EmployeeContacts = new List<SimulatorEmployeeContact>();
            DataIntervalDuringWorkDayCount = 0;
            DataIntervalTotalCount = 0;
            SimulatorDateTime = Configuration.StartDateTime;
            SetInitialEmployeesToWell();
            SetInitialInfectedEmployees();
        }

        /// <summary>
        /// Run the simulator from start to finish using the configuration data.
        /// </summary>
        /// <returns>A simulator result object with status and stats.</returns>
        public SimulatorResult RunNext()
        {
            var result = new SimulatorResult();
            result.SimulatorDateTime = SimulatorDateTime;

            try
            {
                if (!IsSimulationComplete())
                {
                    AdvanceEmployeesVirusStages();
                    HandleTestResults();
                    TestSickEmployees();
                    UpdateEmployeesLocations();
                    if (IsWorkTime())
                    {
                        result.EmployeeContacts = SimulateEmployeeContacts();
                    }
                    EmployeeContacts.AddRange(result.EmployeeContacts);
                    AdvanceSimulatorDateTime();
                }

                result.IsSimulatorComplete = IsSimulationComplete();
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
        public IList<SimulatorEmployeeContact> SimulateEmployeeContacts()
        {
            // Get initial list of all employees.
            IList<SimulatorEmployeeContact> employeeContacts = InitializeEmployeeContacts();
            SimulatorVirusStage infectedStage = Configuration.VirusStages.First(f => f.InfectionStage == SimulatorDataConstant.InfectionStage_Infected);

            // Now we need to retrieve all of the employees that each employee contacted. Exclude employees that are out sick
            foreach (var currentEmployee in (from e in Configuration.Employees
                                             join ec in employeeContacts
                                             on e.Id equals ec.EmployeeId
                                             where !e.IsOutSick
                                             select new { Employee = e, EmployeeContact = ec }))
            {
                // Only check once per contact for each employee.
                bool hasEmployeeBeenChecked = false;

                // Now retrieve all employees in the same room (except the employee herself).
                var contactedEmployees = Configuration.Employees.Where(f => !f.IsOutSick && f.CurrentRoomId == currentEmployee.Employee.CurrentRoomId && f.Id != currentEmployee.Employee.Id);
                bool isEmployeeWell = IsVirusStageWell(currentEmployee.Employee.VirusStageId);
                bool isRoomContagious = IsRoomContagious(currentEmployee.Employee.CurrentRoomId.GetValueOrDefault());

                // Loop through each contacted contacted employee.
                foreach (var contactedEmployee in contactedEmployees)
                {
                    // Always add each contacted employee to the employee's employee contact list.
                    currentEmployee.EmployeeContact.EmployeeContacts.Add(GetEmployeeContactFromEmployee(contactedEmployee));

                    // If the room is contagious and the employee is well adn the contact is contagious, then check to see if an infection occurred.
                    // BUT only check each employee once per hour. Meeting with 1 people or 10 people should give same possible infection rate.
                    if (!hasEmployeeBeenChecked && isRoomContagious && isEmployeeWell && IsVirusStageContagious(contactedEmployee.VirusStageId))
                    {
                        // Check to see if infection caused (random).
                        if (IsContactInfectious())
                        {
                            // Set infected employee to infected stage.
                            SetEmployeeVirusStage(currentEmployee.Employee, infectedStage);
                        }
                        hasEmployeeBeenChecked = true;
                    }
                }
            }

            return employeeContacts;
        }

        public bool IsInfectionComplete()
        {
            int totalInfected = (from e in Configuration.Employees
                                 join v in Configuration.VirusStages
                                 on e.VirusStageId equals v.Id
                                 where v.IsInfected
                                 select e).Count();

            return totalInfected == Configuration.Employees.Count;
        }

        /// <summary>
        /// Allow certain fields to be changed in the configuration for now.
        /// </summary>
        /// <param name="startDate">Start Date of the simulation.</param>
        /// <param name="endDate">End Date of the simulation.</param>
        /// <param name="infectionRate">The infection rate.</param>
        /// <param name="testRate">The virus test rate.</param>
        /// <param name="testResultTime">The time to wait for test results.</param>
        /// <param name="recoveryDays">The recovery days (time off of work).</param>
        public void UpdateConfiguration(DateTime? startDate, DateTime? endDate, decimal? infectionRate, decimal? testRate, TimeSpan? testResultTime, int? recoveryDays, bool canGetSickInOffice = true)
        {
            if (startDate != null)
            {
                Configuration.StartDateTime = SimulatorConfigManager.GetDateBoundary(startDate.Value, Configuration.StartOfWorkday);
            }
            if (endDate != null)
            {
                Configuration.EndDateTime = SimulatorConfigManager.GetDateBoundary(endDate.Value, Configuration.EndOfWorkday);
            }
            if (infectionRate != null)
            {
                Configuration.Virus.InfectionRate = infectionRate.Value;
            }
            if (testRate != null)
            {
                Configuration.Virus.TestRate = testRate.Value;
            }
            if (testResultTime != null && testResultTime != TimeSpan.Zero)
            {
                Configuration.Virus.TestResultWaitTime = testResultTime.Value;
            }
            if (recoveryDays != null)
            {
                Configuration.Virus.RecoveryDays = recoveryDays.Value;
            }

            Configuration.CanGetSickInOffice = canGetSickInOffice;
        }

        public void UpdateConfiguration(SimulatorConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Create a list of employee contact records for every employee.
        /// </summary>
        /// <returns>List of EmployeeContact records for every employee. You can fill it out as needed.</returns>
        private IList<SimulatorEmployeeContact> InitializeEmployeeContacts()
        {
            IList<SimulatorEmployeeContact> employeeContacts = new List<SimulatorEmployeeContact>(Configuration.Employees.Count);

            foreach (var employee in Configuration.Employees)
            {
                employeeContacts.Add(GetEmployeeContactFromEmployee(employee));
            }
            return employeeContacts;
        }

        /// <summary>
        /// Create default employee contact record from a given employee.
        /// </summary>
        /// <param name="employee">The employee to transform to employee contact.</param>
        /// <returns>The default employee contact record for the given employee.</returns>
        private SimulatorEmployeeContact GetEmployeeContactFromEmployee(SimulatorEmployee employee)
        {
            SimulatorEmployeeContact employeeContact = new SimulatorEmployeeContact();
            employeeContact.ContactDateTime = SimulatorDateTime;
            employeeContact.EmployeeId = employee.Id;
            employeeContact.RoomId = employee.IsOutSick ? 0 : employee.CurrentRoomId.GetValueOrDefault();
            employeeContact.VirusStageId = employee.VirusStageId;
            return employeeContact;
        }


        /// <summary>
        /// This advances each employee's virus stage if the employee's time in the current virus stage has elapsed,
        /// and it sets the next scheduled datetime for the virus status to change.
        /// It does not matter if the current time is during work hours.
        /// </summary>
        private void AdvanceEmployeesVirusStages()
        {
            // Update all employees who have a scheduled virus status change at this time (or already past).
            foreach (var employee in Configuration.Employees.Where(f => f.ScheduledVirusStageChangeDateTime <= SimulatorDateTime))
            {            
                IncrementEmployeeVirusStage(employee);
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
            foreach (var employee in Configuration.Employees.Where(f => !f.IsOutSick && IsEmployeeSick(f) && !f.DoNotTest))
            {
                if (DoTestEmployee(employee))
                {
                    employee.InfectionTestDateTime = SimulatorDateTime;
                    employee.InfectiontTestResult = SimulatorDataConstant.InfectionTestResult_Pending;
                }
                else
                {
                    employee.DoNotTest = true;
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
            try
            {
                int minAttendees = Configuration.MinMeetingAttendance;
                int maxAttendees = Configuration.MaxMeetingAttendance;
                
                int meetingAttendanceSize = GetRandomNumber(minAttendees, maxAttendees + 1);

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
                    int idIndex = GetRandomNumber(0, employeesInOffice.Length);
                    if (!meetingAttendees.Contains(idIndex))
                    {
                        meetingAttendees.Add(employeesInOffice[idIndex]);
                    }
                }

                // Finally, update our meeting employees' current room IDs to the selected meeting room.
                foreach (var employeeId in meetingAttendees)
                {
                    var employee = Configuration.Employees.First(f => f.Id == employeeId);
                    employee.CurrentRoomId = meetingRoom.Id;
                }
            }
            catch (Exception exc)
            {
                throw new Exception($"Unable to move employees to meeting room {meetingRoom.Id}: {exc.Message}", exc);
            }

        }

        /// <summary>
        /// Move employee to a breakroom.
        /// </summary>
        /// <param name="employee">The employee to move. The CurrentRoomId will be updated.</param>
        private void MoveEmployeeToBreakroom(SimulatorEmployee employee)
        {
            try
            {
                IList<SimulatorWorkplaceRoom> availableBreakRooms = GetBreakroomsforEmployee(employee);
                if (availableBreakRooms.Count == 1)
                {
                    // If only one break room, then put them right in there.
                    employee.CurrentRoomId = availableBreakRooms[0].Id;
                }
                else
                {
                    // If more than one break room, then pick random one.                    
                    employee.CurrentRoomId = availableBreakRooms[GetRandomNumber(0, availableBreakRooms.Count)].Id;
                }
            }
            catch (Exception exc)
            {
                throw new Exception($"Unable to move employee {employee.Id} to a breakroom: {exc.Message}", exc);
            }
        }

        /// <summary>
        /// This is used to select a break room for an employee. Try to get one on the employee's floor first.
        /// If not possible, then just return the break rooms.
        /// </summary>
        /// <param name="employee">The employee for whom we want to find a breakroom.</param>
        /// <returns>A list of breakrooms that the employee can </returns>
        private IList<SimulatorWorkplaceRoom> GetBreakroomsforEmployee(SimulatorEmployee employee)
        {
            int floorId = Configuration.WorkplaceRooms.First(f => f.Id == employee.OfficeId).FloorId;
            IList<SimulatorWorkplaceRoom> availableBreakRooms = Configuration.WorkplaceRooms.Where(f => f.RoomType == SimulatorDataConstant.WorkplaceRoomType_Breakroom).ToList();
            if (availableBreakRooms.Any(f => f.FloorId == floorId))
            {
                return availableBreakRooms.Where(f => f.FloorId == floorId).ToList();
            }
            else
            {
                return availableBreakRooms;
            }
        }

        /// <summary>
        /// Determine whether simulation can continue.
        /// </summary>
        /// <returns>True if can continue. False if end of simulation has been reached.</returns>
        private bool IsSimulationComplete()
        {
            return SimulatorDateTime >= Configuration.EndDateTime;
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
        private bool IsVirusStageContagious(int virusStageId)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == virusStageId);
            return virusStage != null && virusStage.IsContagious;
        }

        /// <summary>
        /// This determines if the room is contagious. Only the offices may or may not be contagious.
        /// </summary>
        /// <param name="roomType">The type of room.</param>
        /// <returns>True if virus can spread in this room.</returns>
        private bool IsRoomContagious(int roomId)
        {
            // If they can get it in office, they can get it anywhere.
            if (Configuration.CanGetSickInOffice)
            {
                return true;
            }

            string roomType = Configuration.WorkplaceRooms.FirstOrDefault(f => f.Id == roomId)?.RoomType;

            // It can be spread in any room other than an office.
            return roomType != SimulatorDataConstant.WorkplaceRoomType_Office;
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
        private bool IsVirusStageWell(int virusStageId)
        {
            var virusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == virusStageId);
            return virusStage != null && virusStage.InfectionStage == SimulatorDataConstant.InfectionStage_Well;
        }

        /// <summary>
        /// Determine whether to issue a test based on test rate.
        /// <returns>Return true if a test should be issued.</returns>
        /// </summary>
        /// <param name="employee">The employee to test or not test.</param>
        /// <returns>Return true if a test should be issued.</returns>
        private bool DoTestEmployee(SimulatorEmployee employee)
        {
            // Don't test if already waiting for test result.
            if (employee.InfectiontTestResult == SimulatorDataConstant.InfectionTestResult_Pending)
            {
                return false;
            }
            // Pick a random number between 1 and 1000 and see if it's within our test rate. 
            // Example: There is a 25% (.25*1000) chance that the number will be between 1 and 250.
            // Multiply by 1000 instead of 100 just in case test rate is has thousandths place (which is very likely). It'll be more exact.
            int randomNumber = GetRandomNumber(1, 1001);
            return randomNumber <= Convert.ToInt32(Configuration.Virus.TestRate * 1000);
        }

        /// <summary>
        /// Given a certain infection rate, this will determine if the infection was spread.
        /// We're just using the infection rate and checking to see if a random number falls within that number.
        /// This is a little different than saying that the virus infects x% of the people in contact.
        /// </summary>
        /// <returns>True if the virus should be spread.</returns>
        private bool IsContactInfectious()
        {
            // Pick a random number between 1 and 1000 and see if it's within our infection rate. 
            // Example: There is a 25% (.25*1000) chance that the number will be between 1 and 250.
            // Multiply by 1000 instead of 100 just in case rate is has thousandths place (which is very likely).
            int randomNumber = GetRandomNumber(1, 1001);
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
                SetEmployeeVirusStage(employee, newStage);
            }
        }

        private void SetEmployeeVirusStage(SimulatorEmployee employee, SimulatorVirusStage virusStage)
        {
            employee.VirusStageId = virusStage.Id;
            employee.VirusStageLastChangeDateTime = SimulatorDateTime;
            employee.ScheduledVirusStageChangeDateTime = GetNextScheduledVirusStageChangeTime(virusStage);
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
            var randomDays = GetRandomNumber(virusStage.MinDays, virusStage.MaxDays + 1);
            return SimulatorDateTime.AddDays(randomDays);
        }

        /// <summary>
        /// Get a random number from our Random object. We'll be cautious and make sure only one thread is accessing our Random object.
        /// </summary>
        /// <param name="min">This is the min number that can be returned.</param>
        /// <param name="max">Only numbers less than this number can be returned.</param>
        /// <returns>A random integer within the min and max as explained.</returns>
        private int GetRandomNumber(int min, int max)
        {
            int number;
            lock (_randomLockObject)
            {
                number = _random.Next(min, max);
            }
            return number;
        }

        /// <summary>
        /// Set all employee virus stages to "well" virus stage.
        /// </summary>
        private void SetInitialEmployeesToWell()
        {
            int virusStageId = Configuration.VirusStages.FirstOrDefault(f => f.InfectionStage == SimulatorDataConstant.InfectionStage_Well).Id;
            foreach (var employee in Configuration.Employees)
            {
                employee.VirusStageId = virusStageId;
            }
        }

        /// <summary>
        /// Set the virus stage for a number of employees.
        /// </summary>
        /// <param name="employees">The whole employee list. The first employees in the list will be marked as infected.</param>
        /// <param name="infectedCount">The number of people to infect.</param>
        /// <param name="viralStage">The initial viral stage.</param>
        private void SetInitialInfectedEmployees()
        {
            SimulatorVirusStage initialVirusStage = Configuration.VirusStages.FirstOrDefault(f => f.InfectionStage == Configuration.InitialSickStage);

            try
            {
                // Get all employee IDs.
                int[] allEmployeeIds = (from e in Configuration.Employees
                                           select e.Id).ToArray();
                                
                IList<int> initialInfectedEmployeeIds = new List<int>();

                // Grab random employees and add them to infected list.                
                while (initialInfectedEmployeeIds.Count < Configuration.InitialSickCount)
                {
                    int idIndex = GetRandomNumber(0, allEmployeeIds.Length);
                    if (!initialInfectedEmployeeIds.Contains(idIndex))
                    {
                        initialInfectedEmployeeIds.Add(allEmployeeIds[idIndex]);
                    }
                }

                // Finally, set the employees' statuses.
                foreach (var employeeId in initialInfectedEmployeeIds)
                {
                    var employee = Configuration.Employees.First(f => f.Id == employeeId);
                    SimulatorVirusStage employeeVirusStage = Configuration.VirusStages.FirstOrDefault(f => f.Id == employee.VirusStageId);
                    if (employee.VirusStageId != initialVirusStage.Id)
                    {
                        SetEmployeeVirusStage(employee, initialVirusStage);
                    }
                }
            }
            catch (Exception exc)
            {
                throw new Exception($"Unable to set initial infected employees: {exc.Message}", exc);
            }
        }

        #endregion Methods
    }
}