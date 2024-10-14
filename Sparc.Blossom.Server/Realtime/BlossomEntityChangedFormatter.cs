using MessagePack.Formatters;
using MessagePack;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Realtime
{
    public class BlossomEntityChangedFormatter : IMessagePackFormatter<BlossomEntityChanged>
    {
        public BlossomEntityChanged Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            options.Security.DepthStep(ref reader);
            try
            {
                // Read array header
                int count = reader.ReadArrayHeader();

                // Extract fields
                var typeName = reader.ReadString(); 
                var entityType = Type.GetType(typeName); 

                if (entityType == null)
                {
                    throw new InvalidOperationException($"Unknown entity type: {typeName}");
                }

                var entity = (BlossomEntity)MessagePackSerializer.Deserialize(entityType, ref reader, options);
                var changeType = reader.ReadString();

                return new BlossomEntityChanged(entity);
            }
            finally
            {
                reader.Depth--;
            }
        }

        public void Serialize(ref MessagePackWriter writer, BlossomEntityChanged value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);

            var entityType = value.Entity.GetType();
            writer.Write(entityType.AssemblyQualifiedName); 

            MessagePackSerializer.Serialize(entityType, ref writer, value.Entity, options);

        }
    }


}
