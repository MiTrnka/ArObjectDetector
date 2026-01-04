namespace ArObjectDetector.Models;

/// <summary>
/// Reprezentuje jeden detekovaný objekt z YOLO modelu.
/// Obsahuje pozici, velikost, tøídu a confidence score detekce.
/// </summary>
public class YoloDetection
{
    /// <summary>
    /// X souøadnice levého horního rohu bounding boxu
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y souøadnice levého horního rohu bounding boxu
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Šíøka bounding boxu
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Výška bounding boxu
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Skóre jistoty detekce (0-1), vyšší hodnota znamená vyšší jistotu
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// ID tøídy objektu podle COCO datasetu (0-79)
    /// </summary>
    public int ClassId { get; set; }

    /// <summary>
    /// Textový popisek detekovaného objektu (napø. "person", "car")
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
