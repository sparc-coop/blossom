using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V1;

namespace Sparc.Notifications.Twilio
{
    public class TwilioService
    {
        public TwilioService(TwilioConfiguration configuration, ISendGridClient sendGridClient)
        {
            Config = configuration;
            SendGridClient = sendGridClient;
            TwilioClient.Init(configuration.AccountSid, configuration.AuthToken);
        }

        public TwilioConfiguration Config { get; }
        public ISendGridClient SendGridClient { get; }

        public async Task<bool> SendAsync(string phoneOrEmail, string body, string? subject = null)
        {
            if (IsValidEmail(phoneOrEmail))
                return await SendEmailAsync(phoneOrEmail, subject ?? $"{body.Substring(20)}...", body);

            return await SendSmsAsync(phoneOrEmail, body);
        }

        public async Task<PhoneNumberResource> LookupPhoneNumberAsync(string phoneNumber, string? countryCode = null)
        {
            return await PhoneNumberResource.FetchAsync(phoneNumber, countryCode);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string body)
        {
            var message = await MessageResource.CreateAsync(
                body: body,
                from: new global::Twilio.Types.PhoneNumber(Config.FromPhoneNumber),
                to: new global::Twilio.Types.PhoneNumber(phoneNumber)
            );

            if (message.ErrorMessage != null)
                throw new Exception($"Unable to send SMS: {message.ErrorCode} {message.ErrorMessage}");

            return true;
        }

        public async Task<bool> SendEmailAsync(string emailAddresses, string subject, string body, List<(string filename, string base64)>? attachments = null)
        {
            var msg = new SendGridMessage
            {
                From = new EmailAddress(Config.FromEmailAddress, Config.FromName ?? Config.FromEmailAddress),
                Subject = subject,
                PlainTextContent = body,
                HtmlContent = body
            };
            
            foreach (var email in emailAddresses.Split(',', ';').Select(x => x.Trim()))
                msg.AddTo(email);

            var result = await SendGridClient.SendEmailAsync(msg);
            if (!result.IsSuccessStatusCode)
                throw new Exception($"Unable to send email: Error {result.StatusCode}");

            if (attachments?.Count > 0)
            {
                foreach (var (filename, base64) in attachments)
                    msg.AddAttachment(filename, base64);
            }

            return true;
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                static string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }
    }
}

