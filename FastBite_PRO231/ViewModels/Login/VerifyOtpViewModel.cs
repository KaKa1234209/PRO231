using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels;

public class VerifyOtpViewModel
{
    [Required]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mã OTP.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP gồm 6 chữ số.")]
    [Display(Name = "Mã OTP")]
    public string Otp { get; set; } = "";
}