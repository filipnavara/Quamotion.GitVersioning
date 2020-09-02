using System;
using System.Buffers;
using System.IO;
using System.Text.Json;

namespace Quamotion.GitVersioning
{
    public class VersionFile
    {
        public static string GetVersion(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return GetVersion(stream);
            }
        }

        public static string GetVersion(Stream stream)
        {
            string value = null;

            byte[] data = ArrayPool<byte>.Shared.Rent((int)stream.Length);
            stream.Read(data);

            var span = data.AsSpan(0, (int)stream.Length);
            var reader = new Utf8JsonReader(span, isFinalBlock: true, default);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName
                    && reader.ValueTextEquals("version"))
                {
                    reader.Read();
                    value = reader.GetString();
                    break;
                }
            }

            ArrayPool<byte>.Shared.Return(data);

            return value;
        }
    }
}
