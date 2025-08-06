//using Sparc.Blossom;
//using Sparc.MCN.Users;
//using System.ComponentModel.DataAnnotations;

//namespace Sparc.MCN.Messages;
//public class Message : BlossomEntity<string>
//{
//    [Required(ErrorMessage = "Sender is required")]
//    public User Sender { get; set; }
//    [Required(ErrorMessage = "ChatId is required")]

//    public string ChatId { get; set; }
//    [Required(ErrorMessage = "Text is required")]

//    public string Text { get; set; }
//    public DateTime Timestamp { get; set; }

//    public Message(User sender, string chatId, string text, DateTime timestamp) : base(Guid.NewGuid().ToString())
//    {
//        Sender = sender;
//        ChatId = chatId;
//        Text = text;
//        Timestamp = (timestamp != DateTime.UtcNow ? timestamp : DateTime.UtcNow);
//    }
//}