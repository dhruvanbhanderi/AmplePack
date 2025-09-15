using System.ComponentModel.DataAnnotations;

namespace AmplePack.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public List<string> UserRoles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
        
        [Display(Name = "Roles")]
        public List<string>? SelectedRoles { get; set; }
    }
}