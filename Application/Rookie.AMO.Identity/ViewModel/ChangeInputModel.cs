using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Rookie.AMO.Identity.ViewModel
{
    public class ChangeInputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPass { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        [MaxLength(20)]
        public string NewPass { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [MaxLength(20)]
        [Compare("NewPass", ErrorMessage = "Password does not match, please retry.")]
        public string ConfirmPass { get; set; }

        public string ReturnUrl { get; set; }
    }
}
