using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SlaMonitor.Auth.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tenant seçimi zorunludur.")]
        [Display(Name = "Tenant")]
        public string TenantId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        public List<SelectListItem> TenantOptions { get; set; } = new();
    }
}