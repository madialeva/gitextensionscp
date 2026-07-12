namespace GitUI;

/// <summary>
///  Bridges the UI-neutral icon representation of the plugin API (raw PNG bytes)
///  and the GDI+ <see cref="Image"/> type used by this WinForms shell.
/// </summary>
public static class IconDataExtensions
{
    /// <summary>
    ///  Materializes raw PNG bytes into a standalone <see cref="Image"/>, or <see langword="null"/>
    ///  if there is no data or it is not a valid image.
    /// </summary>
    public static Image? ToImage(this byte[]? iconData)
    {
        if (iconData is null || iconData.Length == 0)
        {
            return null;
        }

        try
        {
            using MemoryStream stream = new(iconData);
            using Image image = Image.FromStream(stream);

            // Copy into a standalone Bitmap: GDI+ decodes lazily and would otherwise
            // require the source stream to outlive the Image.
            return new Bitmap(image);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    ///  Encodes an <see cref="Image"/> as PNG bytes for handing over to the plugin API.
    /// </summary>
    public static byte[] ToPngBytes(this Image image)
    {
        using MemoryStream stream = new();
        image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }
}
