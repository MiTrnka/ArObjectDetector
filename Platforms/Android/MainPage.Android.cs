using Android.Graphics;
using System.Diagnostics;
using ArObjectDetector.Services;

namespace ArObjectDetector;

/// <summary>
/// Platform-specifická implementace objektové detekce pro Android.
/// Využívá YOLO v8 model s ONNX Runtime pro offline detekci objektù.
/// </summary>
public partial class MainPage
{
    private YoloInference? _yoloInference;

    /// <summary>
    /// Inicializuje YOLO model z Android assets.
    /// Model je zkopírován do cache pro pøístup z ONNX Runtime.
    /// </summary>
    private void InitializeYolo()
    {
        try
        {
            Debug.WriteLine("=== Initializing YOLO ===");
            
            using var assetManager = Android.App.Application.Context.Assets;
            using var assetFileDescriptor = assetManager?.OpenFd("yolov8n.onnx");
            
            if (assetFileDescriptor == null)
            {
                Debug.WriteLine("!!! YOLO model not found in assets !!!");
                return;
            }

            var cachePath = System.IO.Path.Combine(Android.App.Application.Context.CacheDir!.AbsolutePath, "yolov8n.onnx");
            
            if (!File.Exists(cachePath))
            {
                Debug.WriteLine($"Copying YOLO model to cache: {cachePath}");
                using var inputStream = assetManager?.Open("yolov8n.onnx");
                using var outputStream = File.Create(cachePath);
                inputStream?.CopyTo(outputStream);
                Debug.WriteLine("YOLO model copied successfully");
            }

            _yoloInference = new YoloInference(cachePath);
            _yoloInference.Initialize();
            
            Debug.WriteLine("=== YOLO Initialized Successfully ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!!! YOLO Initialization FAILED !!!");
            Debug.WriteLine($"Error: {ex.Message}");
            Debug.WriteLine($"Stack: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Provede detekci objektù v obrázku pomocí YOLO modelu.
    /// Tato partial metoda je specifická pro Android platformu.
    /// </summary>
    /// <param name="imageBytes">Obrázek jako byte pole (JPEG/PNG)</param>
    /// <param name="width">Šíøka pùvodního obrázku</param>
    /// <param name="height">Výška pùvodního obrázku</param>
    /// <returns>Seznam detekovaných objektù s pozicemi a labely</returns>
    public partial async Task<List<DetectedObjectResult>> DetectObjectsAsync(byte[] imageBytes, int width, int height)
    {
        Debug.WriteLine($"=== DetectObjectsAsync (YOLO) called with {imageBytes?.Length ?? 0} bytes ===");
        
        try
        {
            if (_yoloInference == null)
            {
                InitializeYolo();
                
                if (_yoloInference == null)
                {
                    Debug.WriteLine("!!! YOLO not initialized, returning empty !!!");
                    return new List<DetectedObjectResult>();
                }
            }

            Debug.WriteLine("DetectObjectsAsync: Decoding bitmap...");
            using var bitmap = await BitmapFactory.DecodeByteArrayAsync(imageBytes, 0, imageBytes.Length);
            if (bitmap == null)
            {
                Debug.WriteLine("!!! Bitmap is NULL !!!");
                return new List<DetectedObjectResult>();
            }

            Debug.WriteLine($"DetectObjectsAsync: Bitmap decoded: {bitmap.Width}x{bitmap.Height}");

            Debug.WriteLine("DetectObjectsAsync: Preprocessing image...");
            var pixels = ImagePreprocessor.PreprocessImage(bitmap);
            
            Debug.WriteLine("DetectObjectsAsync: Running YOLO detection...");
            // Zásadní èást, kde se volá YOLO inference, pøedáme pøedzpracované pixely a rozmìry a vrátíme pole detekcí objektù
            // pixels obsahuje normalizované hodnoty pixelù v rozsahu [0,1], obrázek je zmìnìn na ètverec s paddingem v rozlišení 640x640¡a to vždy
            // bitmap.Width, bitmap.Height obsahují pùvodní rozmìry obrázku pøed zmìnou velikosti a to proto, aby bylo možné správnì mapovat detekce zpìt na pùvodní obrázek
            var detections = _yoloInference.Detect(pixels, bitmap.Width, bitmap.Height);
            
            Debug.WriteLine($"DetectObjectsAsync: YOLO found {detections.Count} objects");

            var results = new List<DetectedObjectResult>();
            foreach (var detection in detections)
            {
                Debug.WriteLine($"  - {detection.Label} ({detection.Confidence:F2}) at [{detection.X:F0}, {detection.Y:F0}, {detection.Width:F0}x{detection.Height:F0}]");
                
                results.Add(new DetectedObjectResult
                {
                    Label = detection.Label,
                    Confidence = detection.Confidence,
                    BoundingBox = new Microsoft.Maui.Graphics.Rect(
                        detection.X, 
                        detection.Y, 
                        detection.Width, 
                        detection.Height),
                    ImageWidth = bitmap.Width,
                    ImageHeight = bitmap.Height
                });
            }
            
            Debug.WriteLine($"DetectObjectsAsync: Returning {results.Count} results");
            return results;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("!!! DetectObjectsAsync EXCEPTION !!!");
            Debug.WriteLine($"Type: {ex.GetType().Name}");
            Debug.WriteLine($"Message: {ex.Message}");
            Debug.WriteLine($"Stack: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"Inner: {ex.InnerException.Message}");
            }
            
            return new List<DetectedObjectResult>();
        }
    }
}
