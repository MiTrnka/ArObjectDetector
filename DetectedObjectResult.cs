namespace ArObjectDetector;

// Model pro předání informací o detekovaném objektu
public class DetectedObjectResult
{
    public string Label { get; set; } = "Neznámé";
    public Rect BoundingBox { get; set; }
    public float Confidence { get; set; }

    // Nové vlastnosti pro scaling
    public double ImageWidth { get; set; }
    public double ImageHeight { get; set; }
}