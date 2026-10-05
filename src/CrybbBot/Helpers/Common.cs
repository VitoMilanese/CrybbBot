using System;
using System.Linq;

namespace CrybbBot.Helpers
{
    internal class Common
    {
        public static bool IsTextFile(byte[] data)
        {
            // If contains null byte → almost certainly binary
            if (data.Contains((byte)0))
                return false;

            try
            {
                // Try UTF8 decode
                var text = System.Text.Encoding.UTF8.GetString(data);

                // Check for too many control characters
                int controlChars = text.Count(c =>
                    char.IsControl(c) &&
                    c != '\r' &&
                    c != '\n' &&
                    c != '\t');

                return controlChars < text.Length * 0.01; // less than 1%
            }
            catch
            {
                return false;
            }
        }
    }
}
