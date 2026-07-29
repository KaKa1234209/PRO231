using System.Net;
using System.Net.Mail;

namespace FastBite_PRO231.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode)
    {
        var host = _config["EmailSettings:SmtpHost"]!;
        var port = int.Parse(_config["EmailSettings:SmtpPort"]!);
        var senderEmail = _config["EmailSettings:SenderEmail"]!;
        var senderPassword = _config["EmailSettings:SenderPassword"]!;
        var senderName = _config["EmailSettings:SenderName"] ?? "FastBite";

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(senderEmail, senderPassword),
            EnableSsl = true
        };

        var message = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = "Mã xác thực đặt lại mật khẩu - FastBite",
            Body = $@"
                <p>Xin chào,</p>
                <p>Mã xác thực (OTP) để đặt lại mật khẩu của bạn là:</p>
                <h2 style='letter-spacing: 4px;'>{otpCode}</h2>
                <p>Mã có hiệu lực trong <b>5 phút</b>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
                <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
            ",
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}