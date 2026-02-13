using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using WAMS.Models;
using WAMS.Services;

namespace WAMS.Services
{
	public class SmtpEmailService : IEmailService
	{
		private readonly EmailSettings _settings;

		public SmtpEmailService(IOptions<EmailSettings> settings)
		{
			_settings = settings.Value;
		}

		public async Task SendAsync(string to, string subject, string body)
		{
			try
			{
				using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
				{
					Credentials = new NetworkCredential("mahubookings@gmail.com", "Passwo"),
					EnableSsl = true
				};

				var message = new MailMessage
				{
					From = new MailAddress(_settings.Username, _settings.SenderName),
					Subject = subject,
					Body = body,
					IsBodyHtml = true
				};

				message.To.Add(to);

				await client.SendMailAsync(message);
			}
			catch (SmtpException ex)
			{
				// Log or handle exception
				throw new InvalidOperationException("Failed to send email. See inner exception for details.", ex);
			}
		}
	}
}
