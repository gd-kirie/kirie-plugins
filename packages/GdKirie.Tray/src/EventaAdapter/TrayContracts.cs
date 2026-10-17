using System.Text.Json;
using System.Text.Json.Serialization;
using Eventa;

namespace GdKirie.Tray.EventaAdapter;

internal enum TrayMethod
{
    ResourceClose,
    TrayCreate,
    MenuCreate,
    SubmenuCreate,
    ItemCreate,
    CheckCreate,
    IconCreate,
    PredefinedSeparator,
    PredefinedSectionHeader,
    PredefinedCopy,
    PredefinedCut,
    PredefinedPaste,
    PredefinedPasteAndMatchStyle,
    PredefinedDelete,
    PredefinedSelectAll,
    PredefinedUndo,
    PredefinedRedo,
    PredefinedMinimize,
    PredefinedMaximize,
    PredefinedZoom,
    PredefinedActualSize,
    PredefinedZoomIn,
    PredefinedZoomOut,
    PredefinedFullscreen,
    PredefinedHide,
    PredefinedHideOthers,
    PredefinedShowAll,
    PredefinedCloseWindow,
    PredefinedQuit,
    PredefinedServices,
    PredefinedBringAllToFront,
    PredefinedStartSpeaking,
    PredefinedStopSpeaking,
    PredefinedStartDictation,
    PredefinedEmojiAndSymbols,
    PredefinedAbout,
    TraySetIcon,
    TraySetMenu,
    TraySetTooltip,
    TraySetTitle,
    TraySetVisible,
    TraySetMenuOnLeftClick,
    TraySetMenuOnRightClick,
    TrayShowMenu,
    TrayRect,
    TrayIconIsTemplate,
    TraySetTempDirPath,
    MenuItems,
    MenuAppend,
    MenuPrepend,
    MenuInsert,
    MenuRemove,
    MenuRemoveAt,
    ItemText,
    ItemIsEnabled,
    CheckIsChecked,
    ItemIconIsTemplate,
    ItemSetText,
    ItemSetEnabled,
    ItemSetAccelerator,
    CheckSetChecked,
    ItemSetIcon,
}

internal static class TrayProtocol
{
    internal static readonly Dictionary<TrayMethod, InvokeEventDefinition<JsonElement, JsonElement>> Methods =
        Enum.GetValues<TrayMethod>().ToDictionary(method => method,
            method => new InvokeEventDefinition<JsonElement, JsonElement>(
                $"kirie:tray:{JsonNamingPolicy.KebabCaseLower.ConvertName(method.ToString())}"));
    internal static readonly EventDefinition<JsonElement> Event = new("kirie:tray:event");
}

/// <summary>A browser handle to an independently owned native reference.</summary>
/// <param name="Handle">The handle accepted by this adapter's methods.</param>
/// <param name="Kind">The native object type.</param>
/// <param name="Id">The upstream object ID.</param>
public sealed record TrayResource(string Handle, string Kind, string Id);
internal sealed record RemoteMenuEvent(string Id)
{
    public string Type => "menu";
}

internal sealed record SuccessReply([property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] object? Value)
{
    public bool Ok => true;
}

internal sealed record RemoteError(string Code, string Message);
internal sealed record FailureReply(RemoteError Error)
{
    public bool Ok => false;
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    RespectRequiredConstructorParameters = true, RespectNullableAnnotations = true,
    AllowOutOfOrderMetadataProperties = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(TrayResource))]
[JsonSerializable(typeof(TrayResource[]))]
[JsonSerializable(typeof(TrayRect))]
[JsonSerializable(typeof(TrayIconEvent))]
[JsonSerializable(typeof(RemoteMenuEvent))]
[JsonSerializable(typeof(SuccessReply))]
[JsonSerializable(typeof(FailureReply))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(ushort))]
[JsonSerializable(typeof(HandleRequest))]
[JsonSerializable(typeof(TrayCreateRequest))]
[JsonSerializable(typeof(MenuCreateRequest))]
[JsonSerializable(typeof(SubmenuCreateRequest))]
[JsonSerializable(typeof(ItemCreateRequest))]
[JsonSerializable(typeof(CheckCreateRequest))]
[JsonSerializable(typeof(IconCreateRequest))]
[JsonSerializable(typeof(EmptyRequest))]
[JsonSerializable(typeof(PredefinedSectionHeaderRequest))]
[JsonSerializable(typeof(PredefinedTextRequest))]
[JsonSerializable(typeof(PredefinedAboutRequest))]
[JsonSerializable(typeof(IconRequest))]
[JsonSerializable(typeof(MenuRequest))]
[JsonSerializable(typeof(TooltipRequest))]
[JsonSerializable(typeof(TitleRequest))]
[JsonSerializable(typeof(VisibleRequest))]
[JsonSerializable(typeof(EnabledRequest))]
[JsonSerializable(typeof(PathRequest))]
[JsonSerializable(typeof(ItemsRequest))]
[JsonSerializable(typeof(InsertRequest))]
[JsonSerializable(typeof(RemoveRequest))]
[JsonSerializable(typeof(PositionRequest))]
[JsonSerializable(typeof(TextRequest))]
[JsonSerializable(typeof(CheckedRequest))]
[JsonSerializable(typeof(AcceleratorRequest))]
internal sealed partial class TrayJsonContext : JsonSerializerContext;
