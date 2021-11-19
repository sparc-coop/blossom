namespace Sparc.Notifications.Twilio
{
    public class TwilioConfiguration
    {
        public string? AccountSid { get; set; }
        public string? AuthToken { get; set; }
        public string? FromPhoneNumber { get; set; }
        public string? SendGridApiKey { get; set; }
        public string? FromEmailAddress { get; set; }
        public string? FromName { get; set; }
    }
}
