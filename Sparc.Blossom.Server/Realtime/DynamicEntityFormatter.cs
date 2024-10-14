using MessagePack;
using MessagePack.Formatters;
using Sparc.Blossom.Data;
using System;
using System.Reflection;

namespace Sparc.Blossom.Realtime
{
    public class DynamicEntityFormatter : IMessagePackFormatter<BlossomEntity>
    {
        public void Serialize(ref MessagePackWriter writer, BlossomEntity value, MessagePackSerializerOptions options)
        {
            var type = value.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            writer.WriteMapHeader(properties.Length);

            foreach (var property in properties)
            {
                writer.Write(property.Name);
                var propertyValue = property.GetValue(value);
                // Serialize the value using the appropriate serializer for the property's type
                writer.Write((byte)propertyValue);
            }
        }

        public BlossomEntity Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var count = reader.ReadMapHeader();
            // Create a new instance of BlossomEntity or its derived type using the type name or another mechanism
            var instance = Activator.CreateInstance<BlossomEntity>();

            var properties = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            for (int i = 0; i < count; i++)
            {
                var propertyName = reader.ReadString();
                var property = Array.Find(properties, p => p.Name == propertyName);
                if (property != null)
                {
                    var value = reader.ReadObject();
                    property.SetValue(instance, value);
                }
            }

            return instance;
        }
    }

}
