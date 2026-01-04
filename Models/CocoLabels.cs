namespace ArObjectDetector.Models;

/// <summary>
/// Definice 80 tøíd objektù z COCO datasetu používaných modelem YOLO.
/// COCO (Common Objects in Context) je standardní dataset pro detekci objektù
/// obsahující bìžné objekty jako lidé, vozidla, zvíøata, nábytek atd.
/// </summary>
public static class CocoLabels
{
    /// <summary>
    /// Pole obsahující názvy všech 80 tøíd COCO datasetu v definovaném poøadí.
    /// Index v poli odpovídá ClassId vráceném z YOLO modelu.
    /// </summary>
    public static readonly string[] Labels = new[]
    {
        "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck", "boat",
        "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog",
        "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella",
        "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite",
        "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle",
        "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich",
        "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "couch",
        "potted plant", "bed", "dining table", "toilet", "tv", "laptop", "mouse", "remote",
        "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book",
        "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
    };

    /// <summary>
    /// Vrací textový popisek pro daný ClassId.
    /// </summary>
    /// <param name="classId">ID tøídy (0-79)</param>
    /// <returns>Název tøídy nebo "Unknown" pokud je ID mimo rozsah</returns>
    public static string GetLabel(int classId)
    {
        if (classId >= 0 && classId < Labels.Length)
            return Labels[classId];
        return "Unknown";
    }
}
