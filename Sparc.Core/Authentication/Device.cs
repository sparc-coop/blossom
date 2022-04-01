namespace Sparc.Core
{
    public class Device
    {
        public virtual string? Id { get; set; }
        public virtual string? PushToken { get; set; }
        public string? DeviceType { get; set; }
        public string? Platform { get; set; }
        public string? Idiom { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? Name { get; set; }
        public string? VersionString { get; set; }
    }
}
