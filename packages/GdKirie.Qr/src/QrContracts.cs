using System.Text.Json.Serialization;
using Eventa;

namespace GdKirie.Qr;

internal sealed record EmptyPayload;
internal sealed record GenerateImageRequest(string Text, int Size, string Foreground, string Background);
internal sealed record PngPayload(string PngBase64);
internal sealed record ScanResult(string? Text);

internal static class QrEvents
{
    public static readonly InvokeEventDefinition<PngPayload, GenerateImageRequest> GenerateImage =
        new("kirie:qr:generate-image");
    public static readonly InvokeEventDefinition<ScanResult, PngPayload> ScanImage =
        new("kirie:qr:scan-image");
    public static readonly InvokeEventDefinition<ScanResult, EmptyPayload> Scan =
        new("kirie:qr:scan");
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EmptyPayload))]
[JsonSerializable(typeof(GenerateImageRequest))]
[JsonSerializable(typeof(PngPayload))]
[JsonSerializable(typeof(ScanResult))]
internal sealed partial class QrJsonContext : JsonSerializerContext;
