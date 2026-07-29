using System.ComponentModel.DataAnnotations;

namespace FastBite_PRO231.ViewModels.Login
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; } = "";

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        //Định dạng kiểu VN
        [RegularExpression(@"^(0|\+84)[0-9]{9}$", ErrorMessage = "Số điện thoại phải có 10 số và bắt đầu bằng 0.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = "";

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)] //Bảo mật
        [MinLength(8, ErrorMessage = "Mật khẩu tối thiểu 8 ký tự")]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = "";
    }
}