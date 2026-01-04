namespace ArObjectDetector;

/// <summary>
/// Reprezentuje výsledek detekce jednoho objektu včetně jeho pozice, třídy a jistoty.
/// Používá se pro předávání výsledků mezi platform-specifickým kódem a UI vrstvou.
/// </summary>
public class DetectedObjectResult
{
    /// <summary>
    /// Textový popisek detekovaného objektu (např. "person", "car", "dog")
    /// </summary>
    public string Label { get; set; } = "Neznámé";

    /// <summary>
    /// Bounding box objektu v souřadnicích původního obrázku
    /// </summary>
    public Rect BoundingBox { get; set; }

    /// <summary>
    /// Jistota detekce v rozsahu 0-1, kde 1 = 100% jistota
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Šířka původního obrázku (potřebné pro správné škálování overlay)
    /// </summary>
    public double ImageWidth { get; set; }

    /// <summary>
    /// Výška původního obrázku (potřebné pro správné škálování overlay)
    /// </summary>
    public double ImageHeight { get; set; }
}