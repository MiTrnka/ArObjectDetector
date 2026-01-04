using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using Microsoft.Maui.Graphics;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Objects;
using Xamarin.Google.MLKit.Vision.Objects.Defaults;

namespace ArObjectDetector;

public partial class MainPage
{
    private IObjectDetector? _detector;

    public partial async Task<List<DetectedObjectResult>> DetectObjectsAsync(byte[] imageBytes, int width, int height)
    {
        if (_detector == null)
        {
            // Inicializace bez pádových metod
            var builder = new Xamarin.Google.MLKit.Vision.Objects.Defaults.ObjectDetectorOptions.Builder();
            builder.EnableMultipleObjects();
            builder.EnableClassification();
            _detector = ObjectDetection.GetClient(builder.Build());
        }

        using var bitmap = await BitmapFactory.DecodeByteArrayAsync(imageBytes, 0, imageBytes.Length);
        if (bitmap == null) return new List<DetectedObjectResult>();

        // Rotace 90 je nutná pro správnou detekci v Portrait režimu
        var image = InputImage.FromBitmap(bitmap, 90);

        // Získání výsledků jako obecný Java objekt
        var resultsObj = await _detector.Process(image);
        var output = new List<DetectedObjectResult>();

        if (resultsObj != null)
        {
            // RUČNÍ PŘEVOD: Toto vyřeší tvůj pád "jarray argument has non-array type"
            var javaList = resultsObj.JavaCast<Java.Util.IList>();
            int size = javaList.Size();

            for (int i = 0; i < size; i++)
            {
                var item = javaList.Get(i);
                var obj = item.JavaCast<Xamarin.Google.MLKit.Vision.Objects.DetectedObject>();

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