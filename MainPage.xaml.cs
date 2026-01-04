using Microsoft.Maui.Layouts;
using System.Diagnostics;

namespace ArObjectDetector;

public partial class MainPage : ContentPage
{
    private bool _isDetecting = false;

    public MainPage()
    {
        InitializeComponent();
    }

    public partial Task<List<DetectedObjectResult>> DetectObjectsAsync(byte[] imageBytes, int width, int height);

    private async void OnStartCameraClicked(object sender, EventArgs e)
    {
        try
        {
            // Zkontrolovat aktuální stav oprávnění
            var currentStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            
            if (currentStatus != PermissionStatus.Granted)
            {
                // Požádat o oprávnění
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                
                if (status != PermissionStatus.Granted)
                {
                    StatusLabel.Text = "Kamera nemá oprávnění!";
                    await DisplayAlert("Oprávnění", "Aplikace potřebuje přístup ke kameře", "OK");
                    return;
                }
            }

            StatusLabel.Text = "Kamera startuje...";
            
            // Spustit kameru
            await CameraViewControl.StartCameraPreview(CancellationToken.None);
            
            _isDetecting = true;
            StatusLabel.Text = "Kamera běží";
            
            _ = Task.Run(DetectionLoop);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Chyba: {ex.Message}";
            Debug.WriteLine($"Camera error: {ex}");
        }
    }

    private async Task DetectionLoop()
    {
        while (_isDetecting)
        {
            await Task.Delay(800);
            
            if (!_isDetecting) break;
            
            byte[]? imageBytes = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    using var stream = await CameraViewControl.CaptureImage(CancellationToken.None);
                    if (stream != null)
                    {
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        imageBytes = ms.ToArray();
                        StatusLabel.Text = $"Snímek pořízen ({imageBytes.Length / 1024} KB)";
                    }
                }
                catch (Exception ex) 
                { 
                    StatusLabel.Text = $"Chyba snímání: {ex.Message}"; 
                    Debug.WriteLine($"Capture error: {ex}");
                }
            });

            if (imageBytes != null && _isDetecting)
            {
                try
                {
                    var results = await DetectObjectsAsync(imageBytes, 0, 0);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (results.Any())
                        {
                            StatusLabel.Text = $"DETEKCE: {results.Count} objektů ({results[0].Label})";
                            UpdateVisualOverlay(results);
                        }
                        else
                        {
                            StatusLabel.Text = "ML Kit: Hledám objekty...";
                            OverlayLayout.Children.Clear();
                        }
                    });
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        StatusLabel.Text = $"CHYBA ML Kit: {ex.Message}";
                        Debug.WriteLine($"Detection error: {ex}");
                    });
                }
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Zastavit detekci a kameru při opuštění stránky
        _isDetecting = false;
        
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CameraViewControl.StopCameraPreview();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error stopping camera: {ex}");
        }
    }

    private void UpdateVisualOverlay(List<DetectedObjectResult> results)
    {
        OverlayLayout.Children.Clear();
        // Testovací bod
        OverlayLayout.Children.Add(new BoxView { Color = Colors.Green, WidthRequest = 10, HeightRequest = 10 });

        if (results == null || !results.Any()) return;

        double displayWidth = OverlayLayout.Width;
        double displayHeight = OverlayLayout.Height;

        foreach (var result in results)
        {
            // POZOR: Protože jsme fotku v ML Kitu otočili o 90 stupňů (Portrait),
            // musíme pro výpočet poměru prohodit ImageWidth a ImageHeight.
            double scaleX = displayWidth / result.ImageHeight;
            double scaleY = displayHeight / result.ImageWidth;

            var label = new Label
            {
                Text = $"{result.Label} {result.Confidence:P0}",
                TextColor = Colors.White,
                BackgroundColor = Color.FromRgba(255, 0, 0, 180),
                FontSize = 16,
                Padding = 5
            };

            // Přepočet souřadnic
            var scaledRect = new Rect(
                result.BoundingBox.X * scaleX,
                result.BoundingBox.Y * scaleY,
                result.BoundingBox.Width * scaleX,
                result.BoundingBox.Height * scaleY
            );

            AbsoluteLayout.SetLayoutBounds(label, scaledRect);
            AbsoluteLayout.SetLayoutFlags(label, AbsoluteLayoutFlags.None);
            OverlayLayout.Children.Add(label);
        }
    }
}