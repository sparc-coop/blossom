namespace TodoItems
{
    public interface IRandomStringGenerator
    {
        string GenerateRandomString();
    }

    public class RandomStringGenerator : IRandomStringGenerator
    {
        public string GenerateRandomString()
        {
            // Generate a random string of lowercase characters
            var random = new Random();
            var length = random.Next(5, 10);
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)('a' + random.Next(0, 26));
            }
            return new string(chars);
        }
    }
}