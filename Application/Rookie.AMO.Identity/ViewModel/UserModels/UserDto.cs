using System;
using System.ComponentModel.DataAnnotations;

namespace Rookie.AMO.Identity.ViewModel.UserModels
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }
        [DataType(DataType.Date)]
        public DateTime JoinedDate { get; set; }
        public string Gender { get; set; }
        public string Type { get; set; }
        public string CodeStaff { get; set; }
        public bool Disable { get; set; }
        public string Location { get; set; }
        public string Email { get; set; }
    }
}
