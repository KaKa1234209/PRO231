namespace FastBite_PRO231.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otpCode);
}