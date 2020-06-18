using System;
using System.ComponentModel.DataAnnotations;

namespace WorkplaceOutbreakSimulatorWebApp.Model
{
    public class SimulatorInput
    {

        #region Properties

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required]
        [Display(Name = "Required Sick Leave Days")]
        [Range(5, 30, ErrorMessage = "The Required Sick Leave Days must be between 5 and 30.")]
        public int? RequiredSickLeaveDays { get; set; }

        [Required]
        [Display(Name = "Virus Infection Rate")]
        [Range(1, 100, ErrorMessage = "The Virus Infection Rate must be between 1 and 100.")]
        public int? InfectionRate { get; set; }

        [Required]
        [Display(Name = "Virus Test Rate")]
        [Range(0, 100, ErrorMessage = "The Virus Test Rate must be between 0 and 100.")]
        public int? TestRate { get; set; }

        [Display(Name = "Virus Test Result Wait Days")]
        [Range(0, 31, ErrorMessage = "The Virus Test Result Wait Days must be between 0 and 31.")]
        public int? TestResultWaitDays { get; set; }

        [Display(Name = "Virus Test Result Wait Hours")]
        [Range(0, 23, ErrorMessage = "The Virus Test Result Wait Hours must be between 0 and 23.")]
        public int? TestResultWaitHours { get; set; }

        [Range(1, 1, ErrorMessage = "The Start Date must be before the End Date.")]
        public int IsDateValid
        {
            get
            {
                return StartDate > EndDate ? 0 : 1;
            }
        }

        [Range(1, 1, ErrorMessage = "The Virus Test Result Wait Period must be between 1 hour and 31 days.")]
        public int IsVirusWaitTimeValid
        {
            get
            {
                if ((TestResultWaitDays ?? 0) == 0 && (TestResultWaitHours ?? 0) == 0)
                {
                    return 0;
                }
                return 1;
            }
        }

        #endregion Properties

    }
}
