using System.Text;
using System.Text.Json;

namespace GearUp.Application.Common.Pagination
{
    public sealed class Cursor
    {
        public DateTime CreatedAt { get; init; }
        public Guid Id { get; init; }

        public static string Encode(Cursor c)
        {
            string json = JsonSerializer.Serialize(c);
            byte[] jsonByte = Encoding.UTF8.GetBytes(json);
            return Convert.ToBase64String(jsonByte);
        }

        public static bool TryDecode(string encodedCursor, out Cursor? result)
        {
            result = null;
            if (string.IsNullOrEmpty(encodedCursor))
            {
                return false;
            }

            try
            {
                var bytes = Convert.FromBase64String(encodedCursor);
                var json = Encoding.UTF8.GetString(bytes);
                result = JsonSerializer.Deserialize<Cursor>(json);
                return result != null;
            }
            catch (FormatException ex)
            {
                return false;
            }
            catch (JsonException ex)
            {
                return false;
            }
        }

    }
}