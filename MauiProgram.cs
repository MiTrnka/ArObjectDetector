using CommunityToolkit.Maui;

namespace ArObjectDetector;

/// <summary>
/// ArObjectDetector - Real-time Object Detection Application
/// 
/// PŘEHLED PROJEKTU:
/// Tato .NET MAUI aplikace implementuje real-time detekci objektů pomocí YOLOv8 neuronové sítě
/// běžící offline přímo na Android zařízení. Aplikace zachycuje video stream z kamery,
/// periodicky provádí inferenci pomocí ONNX Runtime a překrývá detekované objekty
/// bounding boxy s popisky přímo na kamerovém náhledu.
/// 
/// ARCHITEKTURA:
/// - MainPage.xaml.cs: UI vrstva s kamerovým náhledem a detekční smyčkou
/// - Platforms/Android/MainPage.Android.cs: Platform-specifická implementace detekce
/// - Services/YoloInference.cs: YOLO v8 inferenční engine s NMS post-processingem
/// - Services/ImagePreprocessor.cs: Preprocessing obrázků (resize, letterboxing, normalizace)
/// - Models/YoloDetection.cs: Datový model pro detekované objekty
/// - Models/CocoLabels.cs: Definice 80 tříd COCO datasetu
/// 
/// YOLO MODEL:
/// Aplikace používá YOLOv8n (nano) model ve formátu ONNX (~12 MB).
/// Model je stažený z: https://storage.googleapis.com/ailia-models/yolov8/yolov8n.onnx
/// Umístění: Platforms/Android/Assets/yolov8n.onnx
/// Model je trénován na COCO datasetu a dokáže detekovat 80 běžných objektů jako:
/// lidé, vozidla (auta, motocykly, autobusy), zvířata (psi, kočky, ptáci),
/// elektronika (telefony, notebooky, TV), nábytek a další běžné předměty.
/// 
/// TECHNOLOGIE:
/// - .NET MAUI 10: Cross-platform framework (aktuálně zaměřeno na Android)
/// - ONNX Runtime: Microsoft ML inference engine pro neuronové sítě
/// - CommunityToolkit.Maui.Camera: Přístup ke kameře zařízení
/// - YOLOv8: State-of-the-art object detection architektura od Ultralytics
/// 
/// WORKFLOW:
/// 1. Uživatel klikne na "Start Camera" a udělí oprávnění
/// 2. Spustí se kontinuální detekční smyčka (800ms interval)
/// 3. Každý frame je:
///    - Zachycen z kamery jako JPEG
///    - Preprocessován (resize na 640x640, letterboxing, normalizace)
///    - Předán YOLO modelu pro inferenci
///    - Post-processován (NMS pro odstranění duplicit)
///    - Vizualizován na overlay vrstvě
/// 
/// VÝKON:
/// - Detekce běží ~1.2 FPS (800ms + inference time)
/// - Model je optimalizován pro mobilní zařízení (nano varianta)
/// - Vše běží offline bez připojení k internetu
/// 
/// POZNÁMKY:
/// - Aplikace zachovává portrait orientaci, bounding boxy jsou správně škálované
/// - Debug výstupy jsou rozsáhlé pro snadné ladění (lze redukovat v produkci)
/// - Model se při prvním spuštění kopíruje z assets do cache pro ONNX Runtime
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Konfiguruje a vytváří MAUI aplikaci s potřebnými službami a toolkity.
    /// </summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // Přidání MAUI Community Toolkit pro rozšířené kontroly a utility
            .UseMauiCommunityToolkit()
            // Přidání Camera podpory z Community Toolkit pro přístup k zařízení kameře
            .UseMauiCommunityToolkitCamera()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        return builder.Build();
    }
}