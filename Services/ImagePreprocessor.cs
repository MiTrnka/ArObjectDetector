using Android.Graphics;
using System.Diagnostics;

namespace ArObjectDetector.Services;

/// <summary>
/// Preprocessing obrázkù pro YOLO model.
/// Provádí resize s letterboxingem a normalizaci pixelù do formátu oèekávaného YOLO modelem.
/// </summary>
public static class ImagePreprocessor
{
    /// <summary>
    /// Pøevede Android Bitmap na normalizované float pole pøipravené pro YOLO inferenci.
    /// Výsledný tensor má rozmìry [3, targetSize, targetSize] s hodnotami v rozsahu [0, 1].
    /// </summary>
    /// <param name="bitmap">Vstupní obrázek</param>
    /// <param name="targetSize">Cílová velikost (640px pro YOLO v8)</param>
    /// <returns>Float pole s RGB kanály normalizovanými na [0, 1]</returns>
    public static float[] PreprocessImage(Bitmap bitmap, int targetSize = 640)
    {
        Debug.WriteLine($"ImagePreprocessor: Input bitmap {bitmap.Width}x{bitmap.Height}");

        var (resizedBitmap, scale, padX, padY) = ResizeWithPadding(bitmap, targetSize);

        Debug.WriteLine($"ImagePreprocessor: Resized to {resizedBitmap.Width}x{resizedBitmap.Height}, scale={scale:F2}, pad=({padX},{padY})");

        var pixels = new float[3 * targetSize * targetSize];
        
        for (int y = 0; y < targetSize; y++)
        {
            for (int x = 0; x < targetSize; x++)
            {
                int pixelValue = resizedBitmap.GetPixel(x, y);
                
                int r = Android.Graphics.Color.GetRedComponent(pixelValue);
                int g = Android.Graphics.Color.GetGreenComponent(pixelValue);
                int b = Android.Graphics.Color.GetBlueComponent(pixelValue);
                
                int index = y * targetSize + x;
                pixels[index] = r / 255.0f;
                pixels[targetSize * targetSize + index] = g / 255.0f;
                pixels[2 * targetSize * targetSize + index] = b / 255.0f;
            }
        }

        if (resizedBitmap != bitmap)
        {
            resizedBitmap.Dispose();
        }

        Debug.WriteLine($"ImagePreprocessor: Preprocessed to {pixels.Length} floats");
        return pixels;
    }

    /// <summary>
    /// Provede resize obrázku s letterboxingem (zachování pomìru stran s èernými pruhy).
    /// Používá se pro zachování proporce objektù pøi zmìnì velikosti na fixní rozmìry.
    /// </summary>
    /// <returns>Tuple obsahující resized bitmap, scale faktor a padding offsety</returns>
    private static (Bitmap resized, float scale, int padX, int padY) ResizeWithPadding(Bitmap bitmap, int targetSize)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        float scale = Math.Min((float)targetSize / width, (float)targetSize / height);

        int newWidth = (int)(width * scale);
        int newHeight = (int)(height * scale);

        var resized = Bitmap.CreateBitmap(targetSize, targetSize, Bitmap.Config.Argb8888!);
        var canvas = new Canvas(resized);
        canvas.DrawColor(Android.Graphics.Color.Black);

        int padX = (targetSize - newWidth) / 2;
        int padY = (targetSize - newHeight) / 2;

        var scaledBitmap = Bitmap.CreateScaledBitmap(bitmap, newWidth, newHeight, true);
        canvas.DrawBitmap(scaledBitmap, padX, padY, null);
        
        scaledBitmap.Dispose();

        return (resized, scale, padX, padY);
    }
}
