using System.Text;

namespace ZatcaEGS.Helpers
{
    public class QrCodeDecoder
    {
        public static Dictionary<int, byte[]> DecodeQRCode(string qrCodeContent)
        {
            byte[] data = Convert.FromBase64String(qrCodeContent);
            return ParseTlvData(data);
        }

        private static Dictionary<int, byte[]> ParseTlvData(byte[] data)
        {
            var result = new Dictionary<int, byte[]>();
            using (var stream = new MemoryStream(data))
            {
                while (stream.Position < stream.Length)
                {
                    int tag = ReadTag(stream);
                    int length = ReadLength(stream);
                    byte[] value = new byte[length];
                    stream.Read(value, 0, length);
                    result.Add(tag, value);
                }
            }
            return result;
        }

        private static int ReadTag(Stream stream)
        {
            int tag = 0;
            int b;
            do
            {
                b = stream.ReadByte();
                if (b == -1) throw new EndOfStreamException("Unexpected end of stream while reading tag");
                tag = tag << 8 | b;
            } while ((b & 0x80) != 0);
            return tag;
        }

        private static int ReadLength(Stream stream)
        {
            int b = stream.ReadByte();
            if (b == -1) throw new EndOfStreamException("Unexpected end of stream while reading length");
            if (b <= 0x7F) return b;
            int byteCount = b & 0x7F;
            int length = 0;
            for (int i = 0; i < byteCount; i++)
            {
                b = stream.ReadByte();
                if (b == -1) throw new EndOfStreamException("Unexpected end of stream while reading length");
                length = length << 8 | b;
            }
            return length;
        }

        private static void PrintByteArrayAsHex(byte[] data)
        {
            const int bytesPerLine = 16;
            for (int i = 0; i < data.Length; i += bytesPerLine)
            {
                Console.Write($"{i:D4}: ");
                for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
                {
                    Console.Write($"{data[i + j]:X2} ");
                }
                Console.WriteLine();
            }
        }

        public static void PrintDecodedContent(Dictionary<int, byte[]> decodedContent)
        {
            foreach (var kvp in decodedContent)
            {
                Console.Write($"Tag: {kvp.Key}, Value: ");
                if (kvp.Key > 7)
                {
                    Console.WriteLine();
                    PrintByteArrayAsHex(kvp.Value);
                }
                else
                {
                    string value = Encoding.UTF8.GetString(kvp.Value);
                    Console.WriteLine(value);
                }
            }
        }

        public static string GetDecodedContentAsString(string qrCodeContent)
        {
            if (!string.IsNullOrEmpty(qrCodeContent))
            {
                var decodedContent = DecodeQRCode(qrCodeContent);

                var result = new StringBuilder();

                foreach (var kvp in decodedContent)
                {
                    result.AppendLine($"Tag: {kvp.Key}, Value: ");
                    if (kvp.Key > 7)
                    {
                        result.AppendLine(BitConverter.ToString(kvp.Value).Replace("-", " "));
                    }
                    else
                    {
                        result.AppendLine(Encoding.UTF8.GetString(kvp.Value));
                    }
                }

                return result.ToString();
            }
            return string.Empty;
        }
    }


}
