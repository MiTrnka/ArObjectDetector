# Staení YOLOv8n ONNX modelu

## Automatické staení (doporuèeno)

Spus tento PowerShell pøíkaz v koøenovém adresáøi projektu:

```powershell
# Vytvoø sloku pokud neexistuje
New-Item -ItemType Directory -Force -Path "Platforms\Android\Assets"

# Stáhni YOLO model (cca 6 MB)
$url = "https://github.com/ultralytics/assets/releases/download/v0.0.0/yolov8n.onnx"
$output = "Platforms\Android\Assets\yolov8n.onnx"

Write-Host "Stahuji YOLOv8n model..." -ForegroundColor Yellow
Invoke-WebRequest -Uri $url -OutFile $output -UserAgent "Mozilla/5.0"
Write-Host "Model staen úspìšnì!" -ForegroundColor Green
Write-Host "Velikost: $((Get-Item $output).Length / 1MB) MB" -ForegroundColor Cyan
```

## Manuální staení

1. Jdi na: https://github.com/ultralytics/assets/releases
2. Najdi `yolov8n.onnx` (cca 6 MB)
3. Ulo do: `Platforms\Android\Assets\yolov8n.onnx`

## Po staení

V souboru `ArObjectDetector.csproj` **odkomentuj** øádek:

```xml
<ItemGroup>
    <!-- Uncomment when yolov8n.onnx is downloaded -->
    <AndroidAsset Include="Platforms\Android\Assets\yolov8n.onnx" />
</ItemGroup>
```

Zmìò na:

```xml
<ItemGroup>
    <AndroidAsset Include="Platforms\Android\Assets\yolov8n.onnx" />
</ItemGroup>
```

Pak znovu zkompiluj projekt.

## Co model detekuje?

YOLOv8n umí detekovat 80 objektù COCO datasetu:
- Lidé, auta, kola, motorky
- Psi, koèky, konì, ptáci
- Telefony, notebooky, myši, klávesnice
- idle, gauèe, postele, stoly
- A mnoho dalšího!
