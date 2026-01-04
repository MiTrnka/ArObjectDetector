using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using Microsoft.Maui.Graphics;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Objects;
// Důležitý namespace pro standardní ObjectDetectorOptions
using Xamarin.Google.MLKit.Vision.Objects.Defaults;

namespace ArObjectDetector;

public partial class MainPage
{
    private IObjectDetector? _detector;

    // Metoda pro detekci objektů využívající ML Kit na platformě Android
    public partial async Task<List<DetectedObjectResult>> DetectObjectsAsync(byte[] imageBytes, int width, int height)
    {
        // Inicializace detektoru s korektním nastavením
        if (_detector == null)
        {
            // Vytvoření builderu explicitně z namespace Defaults
            var builder = new ObjectDetectorOptions.Builder();

            // Metody voláme samostatně, protože v C# bindingu vrací typ Java.Lang.Object
            // a řetězení (Fluent API) by způsobilo chybu kompilace
            builder.SetDetectorMode(ObjectDetectorOptions.StreamMode);
            builder.EnableMultipleObjects();
            builder.EnableClassification();

            // Sestavení konfigurace a získání klienta
            var options = (ObjectDetectorOptions)builder.Build();
            _detector = ObjectDetection.GetClient(options);
        }

        // Převod bytů na Bitmapu
        using var bitmap = await BitmapFactory.DecodeByteArrayAsync(imageBytes, 0, imageBytes.Length);
        if (bitmap == null) return new List<DetectedObjectResult>();

        // Příprava InputImage (ML Kit vyžaduje otočení pro Portrait orientaci)
        var image = InputImage.FromBitmap(bitmap, 90);

        // Zpracování obrazu
        var resultsObj = await _detector.Process(image);
        var output = new List<DetectedObjectResult>();

        if (resultsObj != null)
        {
            // Převod Java seznamu výsledků na C# kolekci pomocí JavaCast
            var javaList = resultsObj.JavaCast<Java.Util.IList>();
            int size = javaList.Size();

            for (int i = 0; i < size; i++)
            {
                var item = javaList.Get(i);
                var obj = item.JavaCast<Xamarin.Google.MLKit.Vision.Objects.DetectedObject>();

                // Získání prvního nalezeného štítku (např. "Fashon good", "Food" atd.)
                var firstLabel = obj.Labels.FirstOrDefault();

                output.Add(new DetectedObjectResult
                {
                    Label = firstLabel?.Text ?? "Objekt",
                    Confidence = firstLabel?.Confidence ?? 0,
                    BoundingBox = new Microsoft.Maui.Graphics.Rect(
                        obj.BoundingBox.Left, obj.BoundingBox.Top,
                        obj.BoundingBox.Width(), obj.BoundingBox.Height()),
                    ImageWidth = bitmap.Width,
                    ImageHeight = bitmap.Height
                });
            }
        }
        return output;
    }
}