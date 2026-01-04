using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;

namespace ArObjectDetector.Services;

/// <summary>
/// Implementace YOLO v8 objektové detekce pomocí ONNX Runtime.
/// Zpracovává naèítání modelu, inferenci a post-processing výsledkù vèetnì NMS.
/// </summary>
public class YoloInference : IDisposable
{
    private InferenceSession? _session;
    private readonly string _modelPath;
    
    private const int InputSize = 640;
    private const float ConfidenceThreshold = 0.25f;
    private const float IouThreshold = 0.45f;

    /// <summary>
    /// Inicializuje novou instanci YOLO inference enginu.
    /// </summary>
    /// <param name="modelPath">Cesta k .onnx souboru s YOLO modelem</param>
    public YoloInference(string modelPath)
    {
        _modelPath = modelPath;
    }

    /// <summary>
    /// Naète a inicializuje ONNX model pro inferenci.
    /// Musí být zavolána pøed použitím metody Detect().
    /// </summary>
    public void Initialize()
    {
        try
        {
            Debug.WriteLine($"YoloInference: Loading model from {_modelPath}");
            
            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            // Zde naèteme model z dané cesty do InferenceSession
            _session = new InferenceSession(_modelPath, options);
            
            Debug.WriteLine("YoloInference: Model loaded successfully");
            
            foreach (var input in _session.InputMetadata)
            {
                Debug.WriteLine($"  Input: {input.Key}, Shape: {string.Join("x", input.Value.Dimensions)}");
            }
            foreach (var output in _session.OutputMetadata)
            {
                Debug.WriteLine($"  Output: {output.Key}, Shape: {string.Join("x", output.Value.Dimensions)}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"YoloInference ERROR: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Provede detekci objektù na preprocessovaném obrázku.
    /// </summary>
    /// <param name="pixels">Preprocessovaná data obrázku ve formátu [3, 640, 640] normalizovaná na [0,1]</param>
    /// <param name="originalWidth">Pùvodní šíøka obrázku pro škálování výsledkù</param>
    /// <param name="originalHeight">Pùvodní výška obrázku pro škálování výsledkù</param>
    /// <returns>Seznam detekovaných objektù po aplikaci NMS</returns>
    public List<Models.YoloDetection> Detect(float[] pixels, int originalWidth, int originalHeight)
    {
        if (_session == null)
        {
            Debug.WriteLine("YoloInference: Session not initialized!");
            return new List<Models.YoloDetection>();
        }

        try
        {
            var inputTensor = new DenseTensor<float>(pixels, new[] { 1, 3, InputSize, InputSize });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("images", inputTensor)
            };

            Debug.WriteLine("YoloInference: Running inference...");
            using var results = _session.Run(inputs);
            
            var outputTensor = results.First().AsEnumerable<float>().ToArray();
            Debug.WriteLine($"YoloInference: Got {outputTensor.Length} output values");

            var detections = ParseYoloOutput(outputTensor, originalWidth, originalHeight);
            Debug.WriteLine($"YoloInference: Parsed {detections.Count} detections");

            var finalDetections = ApplyNMS(detections, IouThreshold);
            Debug.WriteLine($"YoloInference: After NMS: {finalDetections.Count} detections");

            return finalDetections;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"YoloInference Detect ERROR: {ex.Message}");
            Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return new List<Models.YoloDetection>();
        }
    }

    /// <summary>
    /// Parsuje výstup z YOLO modelu a pøevádí ho na seznam detekcí.
    /// YOLO v8 vrací tensor ve tvaru [1, 84, 8400] kde 84 = 4 bbox souøadnice + 80 tøíd.
    /// </summary>
    private List<Models.YoloDetection> ParseYoloOutput(float[] output, int originalWidth, int originalHeight)
    {
        var detections = new List<Models.YoloDetection>();
        
        int numClasses = 80;
        int numPredictions = 8400;

        float scaleX = (float)originalWidth / InputSize;
        float scaleY = (float)originalHeight / InputSize;

        for (int i = 0; i < numPredictions; i++)
        {
            float x = output[i] * scaleX;
            float y = output[numPredictions + i] * scaleY;
            float w = output[2 * numPredictions + i] * scaleX;
            float h = output[3 * numPredictions + i] * scaleY;

            float maxScore = 0;
            int maxClassId = 0;

            for (int c = 0; c < numClasses; c++)
            {
                float score = output[(4 + c) * numPredictions + i];
                if (score > maxScore)
                {
                    maxScore = score;
                    maxClassId = c;
                }
            }

            if (maxScore > ConfidenceThreshold)
            {
                detections.Add(new Models.YoloDetection
                {
                    X = x - w / 2,
                    Y = y - h / 2,
                    Width = w,
                    Height = h,
                    Confidence = maxScore,
                    ClassId = maxClassId,
                    Label = Models.CocoLabels.GetLabel(maxClassId)
                });
            }
        }

        return detections;
    }

    /// <summary>
    /// Aplikuje Non-Maximum Suppression pro odstranìní duplicitních detekcí.
    /// Ponechá pouze detekce s nejvyšším confidence score pro pøekrývající se objekty.
    /// </summary>
    private List<Models.YoloDetection> ApplyNMS(List<Models.YoloDetection> detections, float iouThreshold)
    {
        var result = new List<Models.YoloDetection>();
        var sorted = detections.OrderByDescending(d => d.Confidence).ToList();

        while (sorted.Any())
        {
            var best = sorted.First();
            result.Add(best);
            sorted.RemoveAt(0);

            sorted = sorted.Where(d =>
            {
                if (d.ClassId != best.ClassId)
                    return true;

                float iou = CalculateIoU(best, d);
                return iou < iouThreshold;
            }).ToList();
        }

        return result;
    }

    /// <summary>
    /// Vypoèítá Intersection over Union (IoU) mezi dvìma bounding boxy.
    /// IoU je metrika pøekryvu mezi 0 (žádný pøekryv) a 1 (úplný pøekryv).
    /// </summary>
    private float CalculateIoU(Models.YoloDetection a, Models.YoloDetection b)
    {
        float x1 = Math.Max(a.X, b.X);
        float y1 = Math.Max(a.Y, b.Y);
        float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
        float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

        float intersectionArea = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
        float areaA = a.Width * a.Height;
        float areaB = b.Width * b.Height;
        float unionArea = areaA + areaB - intersectionArea;

        return unionArea > 0 ? intersectionArea / unionArea : 0;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }
}
