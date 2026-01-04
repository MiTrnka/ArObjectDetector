using Microsoft.Maui.Layouts;
using System.Diagnostics;

namespace ArObjectDetector;

/// <summary>
/// Hlavní stránka aplikace pro objektovou detekci v reálném čase.
/// Zobrazuje kamerový náhled a překrývá detekované objekty bounding boxy s popisky.
/// </summary>
public partial class MainPage : ContentPage
{
    private bool _isDetecting = false;

    public MainPage()
    {
        InitializeComponent();
        Debug.WriteLine("MainPage: Constructor initialized");
    }

    /// <summary>
    /// Platform-specifická metoda pro detekci objektů.
    /// Implementace je v Platforms/Android/MainPage.Android.cs
    /// </summary>
    public partial Task<List<DetectedObjectResult>> DetectObjectsAsync(byte[] imageBytes, int width, int height);

    /// <summary>
    /// Handler pro kliknutí na tlačítko Start Camera.
    /// Vyžádá oprávnění a spustí kontinuální detekční smyčku.
    /// </summary>
    private async void OnStartCameraClicked(object sender, EventArgs e)
    {
        Debug.WriteLine("=== OnStartCameraClicked: Button clicked ===");
        try
        {
            var currentStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            Debug.WriteLine($"Camera permission status: {currentStatus}");
            
            if (currentStatus != PermissionStatus.Granted)
            {
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                Debug.WriteLine($"Camera permission requested, result: {status}");
                
                if (status != PermissionStatus.Granted)
                {
                    StatusLabel.Text = "Kamera nemá oprávnění!";
                    await DisplayAlert("Oprávnění", "Aplikace potřebuje přístup ke kameře", "OK");
                    return;
                }
            }

            StatusLabel.Text = "Kamera startuje...";
            Debug.WriteLine("Starting camera preview...");
            
            await CameraViewControl.StartCameraPreview(CancellationToken.None);
            
            _isDetecting = true;
            StatusLabel.Text = "Kamera běží";
            Debug.WriteLine("=== Camera started, launching DetectionLoop ===");
            
            _ = Task.Run(DetectionLoop);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Chyba: {ex.Message}";
            Debug.WriteLine($"Camera error: {ex}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Kontinuální detekční smyčka běžící na pozadí.
    /// Každých 800ms zachytí frame z kamery, provede detekci a aktualizuje UI.
    /// </summary>
    private async Task DetectionLoop()
    {
        Debug.WriteLine("=== DetectionLoop STARTED ===");
        int loopCount = 0;
        
        while (_isDetecting)
        {
            loopCount++;
            Debug.WriteLine($"--- DetectionLoop iteration #{loopCount} ---");
            
            await Task.Delay(800);
            
            if (!_isDetecting)
            {
                Debug.WriteLine("DetectionLoop: _isDetecting = false, breaking");
                break;
            }
            
            byte[]? imageBytes = null;

            Debug.WriteLine("DetectionLoop: Capturing image...");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    Debug.WriteLine("DetectionLoop: Calling CaptureImage");
                    using var stream = await CameraViewControl.CaptureImage(CancellationToken.None);
                    if (stream != null)
                    {
                        Debug.WriteLine($"DetectionLoop: Stream received, length: {stream.Length}");
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        imageBytes = ms.ToArray();
                        Debug.WriteLine($"DetectionLoop: Image captured, size: {imageBytes.Length} bytes ({imageBytes.Length / 1024} KB)");
                        StatusLabel.Text = $"Snímek pořízen ({imageBytes.Length / 1024} KB)";
                    }
                    else
                    {
                        Debug.WriteLine("DetectionLoop: Stream is NULL!");
                    }
                }
                catch (Exception ex) 
                { 
                    StatusLabel.Text = $"Chyba snímání: {ex.Message}"; 
                    Debug.WriteLine($"Capture error: {ex}");
                    Debug.WriteLine($"Capture error stack: {ex.StackTrace}");
                }
            });

            if (imageBytes != null && _isDetecting)
            {
                Debug.WriteLine($"DetectionLoop: Calling DetectObjectsAsync with {imageBytes.Length} bytes");
                try
                {
                    var results = await DetectObjectsAsync(imageBytes, 0, 0);

                    Debug.WriteLine($"DetectionLoop: DetectObjectsAsync returned {results?.Count ?? 0} results");
                    
                    if (results != null)
                    {
                        foreach (var result in results)
                        {
                            Debug.WriteLine($"  - Result: {result.Label}, Confidence: {result.Confidence:F2}, BBox: [{result.BoundingBox.X}, {result.BoundingBox.Y}, {result.BoundingBox.Width}, {result.BoundingBox.Height}]");
                        }
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (results != null && results.Any())
                        {
                            StatusLabel.Text = $"DETEKCE: {results.Count} objektů ({results[0].Label})";
                            Debug.WriteLine($"DetectionLoop: Updating UI with {results.Count} objects");
                            UpdateVisualOverlay(results);
                        }
                        else
                        {
                            StatusLabel.Text = "YOLO: Hledám objekty...";
                            Debug.WriteLine("DetectionLoop: No objects detected, clearing overlay");
                            OverlayLayout.Children.Clear();
                        }
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"!!! DetectionLoop: EXCEPTION in DetectObjectsAsync !!!");
                    Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                    Debug.WriteLine($"Exception message: {ex.Message}");
                    Debug.WriteLine($"Exception stack: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        Debug.WriteLine($"Inner stack: {ex.InnerException.StackTrace}");
                    }
                    
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        StatusLabel.Text = $"CHYBA YOLO: {ex.Message}";
                    });
                }
            }
            else
            {
                Debug.WriteLine($"DetectionLoop: Skipping detection (imageBytes null: {imageBytes == null}, isDetecting: {_isDetecting})");
            }
        }
        
        Debug.WriteLine("=== DetectionLoop ENDED ===");
    }

    /// <summary>
    /// Lifecycle metoda - ukončí detekci a kameru při opuštění stránky.
    /// </summary>
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        Debug.WriteLine("MainPage: OnDisappearing called");
        
        _isDetecting = false;
        
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CameraViewControl.StopCameraPreview();
                Debug.WriteLine("MainPage: Camera stopped");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping camera: {ex}");
        }
    }

    /// <summary>
    /// Aktualizuje vizuální overlay s bounding boxy pro detekované objekty.
    /// Škáluje souřadnice z obrázku na velikost displeje a vykresluje červené rámečky.
    /// </summary>
    private void UpdateVisualOverlay(List<DetectedObjectResult> results)
    {
        Debug.WriteLine($"=== UpdateVisualOverlay called with {results?.Count ?? 0} results ===");
        
        OverlayLayout.Children.Clear();

        if (results == null || !results.Any())
        {
            Debug.WriteLine("UpdateVisualOverlay: No results to display");
            return;
        }

        // Počkej až bude OverlayLayout připravený
        if (OverlayLayout.Width <= 0 || OverlayLayout.Height <= 0)
        {
            Debug.WriteLine($"UpdateVisualOverlay: OverlayLayout není připravený: {OverlayLayout.Width}x{OverlayLayout.Height}");
            
            // Zkus znovu po layoutu
            Dispatcher.Dispatch(() =>
            {
                if (OverlayLayout.Width > 0 && OverlayLayout.Height > 0)
                {
                    Debug.WriteLine($"UpdateVisualOverlay: Retry after layout, size: {OverlayLayout.Width}x{OverlayLayout.Height}");
                    UpdateVisualOverlay(results);
                }
            });
            return;
        }

        double displayWidth = OverlayLayout.Width;
        double displayHeight = OverlayLayout.Height;

        Debug.WriteLine($"UpdateVisualOverlay: Display={displayWidth}x{displayHeight}, Results={results.Count}");

        foreach (var result in results)
        {
            Debug.WriteLine($"Object: {result.Label} ({result.Confidence:P0}) at [{result.BoundingBox.X}, {result.BoundingBox.Y}, {result.BoundingBox.Width}, {result.BoundingBox.Height}], Image={result.ImageWidth}x{result.ImageHeight}");

            // POZOR: Protože jsme fotku v ML Kitu otočili o 90 stupňů (Portrait),
            // musíme pro výpočet poměru prohodit ImageWidth a ImageHeight. 
            double scaleX = displayWidth / result.ImageHeight;
            double scaleY = displayHeight / result.ImageWidth;

            Debug.WriteLine($"Scale: X={scaleX}, Y={scaleY}");

            // Přepočet souřadnic
            var scaledX = result.BoundingBox.X * scaleX;
            var scaledY = result.BoundingBox.Y * scaleY;
            var scaledWidth = result.BoundingBox.Width * scaleX;
            var scaledHeight = result.BoundingBox.Height * scaleY;

            Debug.WriteLine($"Scaled: [{scaledX}, {scaledY}, {scaledWidth}, {scaledHeight}]");

            var border = new Border
            {
                Stroke = Colors.Red,
                StrokeThickness = 3,
                BackgroundColor = Colors.Transparent,
                Content = new Label
                {
                    Text = $"{result.Label} {result.Confidence:P0}",
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromRgba(255, 0, 0, 180),
                    FontSize = 14,
                    Padding = new Thickness(5, 2),
                    VerticalOptions = LayoutOptions.Start,
                    HorizontalOptions = LayoutOptions.Start
                }
            };

            var scaledRect = new Rect(scaledX, scaledY, scaledWidth, scaledHeight);

            AbsoluteLayout.SetLayoutBounds(border, scaledRect);
            AbsoluteLayout.SetLayoutFlags(border, AbsoluteLayoutFlags.None);
            OverlayLayout.Children.Add(border);
            
            Debug.WriteLine($"Added border to overlay at {scaledRect}");
        }
        
        Debug.WriteLine($"Total overlay children: {OverlayLayout.Children.Count}");
    }
}