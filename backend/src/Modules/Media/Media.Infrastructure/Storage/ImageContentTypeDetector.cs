namespace Media.Infrastructure.Storage;

/// <summary>Magic-byte tabanlı görsel MIME tespiti.</summary>
internal static class ImageContentTypeDetector
{
    private const string Jpeg = "image/jpeg";
    private const string Png = "image/png";
    private const string Webp = "image/webp";

    public static string? Detect(Stream content)
    {
        if (!content.CanRead)
        {
            return null;
        }

        Span<byte> header = stackalloc byte[12];
        var read = content.Read(header);
        if (read < 3)
        {
            ResetStream(content);
            return null;
        }

        string? detected = null;

        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            detected = Jpeg;
        }
        else if (read >= 8 &&
                 header[0] == 0x89 &&
                 header[1] == (byte)'P' &&
                 header[2] == (byte)'N' &&
                 header[3] == (byte)'G' &&
                 header[4] == 0x0D &&
                 header[5] == 0x0A &&
                 header[6] == 0x1A &&
                 header[7] == 0x0A)
        {
            detected = Png;
        }
        else if (read >= 12 &&
                 header[0] == (byte)'R' &&
                 header[1] == (byte)'I' &&
                 header[2] == (byte)'F' &&
                 header[3] == (byte)'F' &&
                 header[8] == (byte)'W' &&
                 header[9] == (byte)'E' &&
                 header[10] == (byte)'B' &&
                 header[11] == (byte)'P')
        {
            detected = Webp;
        }

        ResetStream(content);
        return detected;
    }

    private static void ResetStream(Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }
    }
}
