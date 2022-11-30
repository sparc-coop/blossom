using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Globalization;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Http;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Lookups.V1;

namespace Sparc.Notifications.Twilio;

public class TwilioService
{
    public TwilioService(TwilioConfiguration configuration, ISendGridClient sendGridClient)
    {
        Config = configuration;
        SendGridClient = sendGridClient;
        
        if (configuration.AccountSid != null && configuration.AuthToken != null)
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

    public async Task<bool> SendEmailAsync(string emailAddresses, string subject, string body, string? fromEmailAddress = null, List<(string filename, string base64)>? attachments = null)
    {
        if (fromEmailAddress == null)
            fromEmailAddress = Config.FromEmailAddress;

        var msg = new SendGridMessage
        {
            From = new EmailAddress(fromEmailAddress, Config.FromName ?? fromEmailAddress),
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

    public async Task<bool> SendEmailTemplateAsync(string toEmail, string templateId, object templateData, string? fromEmailAddress = null)
    {
        fromEmailAddress ??= Config.FromEmailAddress;

        var message = MailHelper.CreateSingleTemplateEmail(
           new EmailAddress(fromEmailAddress, Config.FromName ?? fromEmailAddress),
           new EmailAddress(toEmail),
           templateId,
           templateData);

        var result = await SendGridClient.SendEmailAsync(message);
        if (!result.IsSuccessStatusCode)
            throw new Exception($"Unable to send email: Error {result.StatusCode}");

        return true;
    }

    public async Task AddContactAsync(string emailAddress, string listId, Dictionary<string, string>? customProperties = null)
    {
        var request = JsonConvert.SerializeObject(new
        {
            list_ids = new[] { listId },
            contacts = new[] { new { email = emailAddress, custom_fields = customProperties } }
        });

        var response = await SendGridClient.RequestAsync(
            method: BaseClient.Method.PUT,
            urlPath: "marketing/contacts",
            requestBody: request);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Unable to add contact: Error {response.StatusCode}");
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

