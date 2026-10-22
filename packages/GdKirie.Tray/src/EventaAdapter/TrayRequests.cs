using System.Text.Json;
using System.Text.Json.Serialization;

namespace GdKirie.Tray.EventaAdapter;

internal sealed record HandleRequest(string Handle);
internal sealed record TrayCreateRequest(RemoteTrayOptions Options);
internal sealed record MenuCreateRequest(RemoteMenuOptions Options);
internal sealed record SubmenuCreateRequest(RemoteSubmenuOptions Options);
internal sealed record ItemCreateRequest(RemoteItemOptions Options);
internal sealed record CheckCreateRequest(RemoteCheckOptions Options);
internal sealed record IconCreateRequest(RemoteIconOptions Options);
internal sealed record EmptyRequest;
internal sealed record PredefinedSectionHeaderRequest(string Text);
internal sealed record PredefinedTextRequest(string? Text = null);
internal sealed record PredefinedAboutRequest(string? Text = null, RemoteAboutMetadata? Metadata = null);
internal sealed record IconRequest(string Handle, RemoteIcon? Icon, bool IconAsTemplate = false);
internal sealed record MenuRequest(string Handle, string? Menu);
internal sealed record TooltipRequest(string Handle, string? Tooltip);
internal sealed record TitleRequest(string Handle, string? Title);
internal sealed record VisibleRequest(string Handle, bool Visible);
internal sealed record EnabledRequest(string Handle, bool Enabled);
internal sealed record PathRequest(string Handle, string? Path);
internal sealed record ItemsRequest(string Handle, string[] Items);
internal sealed record InsertRequest(string Handle, string[] Items, int Position);
internal sealed record RemoveRequest(string Handle, string Item);
internal sealed record PositionRequest(string Handle, int Position);
internal sealed record TextRequest(string Handle, string Text);
internal sealed record CheckedRequest(string Handle, bool Checked);
internal sealed record AcceleratorRequest(string Handle, RemoteAccelerator? Accelerator);

internal abstract record RemoteOptions
{
    public string? Id { get; init; }
}

internal sealed record RemoteTrayOptions : RemoteOptions
{
    public RemoteIcon? Icon { get; init; }
    public bool IconAsTemplate { get; init; }
    public string? Menu { get; init; }
    public string? Tooltip { get; init; }
    public string? Title { get; init; }
    public bool? MenuOnLeftClick { get; init; }
    public bool? MenuOnRightClick { get; init; }
    public string? Guid { get; init; }
    public string? AutosaveName { get; init; }
    public string? TempDirPath { get; init; }
}

// Items and Enabled use setters because source-generated JSON overwrites init-only defaults.
// https://github.com/dotnet/runtime/issues/84484
internal sealed record RemoteMenuOptions : RemoteOptions
{
    public string[] Items { get; set; } = [];
}

internal sealed record RemoteSubmenuOptions(string Text) : RemoteOptions
{
    public bool Enabled { get; set; } = true;
    public string[] Items { get; set; } = [];
    public RemoteIcon? Icon { get; init; }
    public bool IconAsTemplate { get; init; }
}

internal record RemoteItemOptions(string Text) : RemoteOptions
{
    public bool Enabled { get; set; } = true;
    public RemoteAccelerator? Accelerator { get; init; }
}

internal sealed record RemoteCheckOptions(string Text) : RemoteItemOptions(Text)
{
    public bool Checked { get; init; }
}

internal sealed record RemoteIconOptions(string Text) : RemoteItemOptions(Text)
{
    public RemoteIcon? Icon { get; init; }
    public bool IconAsTemplate { get; init; }
}

internal sealed record RemoteAccelerator(string Kind, string Value)
{
    internal Accelerator ToSdk() => Kind switch
    {
        "physical" => new Accelerator.Physical(Value),
        "logical" => new Accelerator.Logical(Value),
        _ => throw new TrayException("invalid-argument", $"Unknown accelerator kind: {Kind}"),
    };
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(RemoteIcon.ResourceTexture), "resource")]
[JsonDerivedType(typeof(RemoteIcon.RgbaPixels), "rgba")]
[JsonDerivedType(typeof(RemoteIcon.Native), "native")]
[JsonDerivedType(typeof(RemoteIcon.NativeName), "native-name")]
[JsonDerivedType(typeof(RemoteIcon.WindowsStock), "windows-stock")]
[JsonDerivedType(typeof(RemoteIcon.WindowsPath), "windows-path")]
[JsonDerivedType(typeof(RemoteIcon.WindowsResource), "windows-resource")]
internal abstract record RemoteIcon
{
    internal abstract IconSource ToSdk();

    internal sealed record ResourceTexture(string Path, double Scale = 1) : RemoteIcon
    {
        internal override IconSource ToSdk() => new IconSource.Resource(Path, Scale);
    }

    internal sealed record RgbaPixels(List<byte> Rgba, uint Width, uint Height, double Scale = 1) : RemoteIcon
    {
        internal override IconSource ToSdk() => new IconSource.Rgba(Rgba.ToArray(), Width, Height, Scale);
    }

    internal sealed record Native(string Name) : RemoteIcon
    {
        internal override IconSource ToSdk() => new IconSource.Native(Name);
    }

    internal sealed record NativeName(string Name) : RemoteIcon
    {
        internal override IconSource ToSdk() => new IconSource.NativeName(Name);
    }

    internal sealed record WindowsStock(int Id) : RemoteIcon
    {
        internal override IconSource ToSdk() => new IconSource.WindowsStock(Id);
    }

    internal sealed record WindowsPath(string Path, uint? Width = null, uint? Height = null) : RemoteIcon
    {
        internal override IconSource ToSdk() => new IconSource.WindowsPath(Path, Width, Height);
    }

    internal sealed record WindowsResource(JsonElement Resource, uint? Width = null, uint? Height = null) : RemoteIcon
    {
        internal override IconSource ToSdk() => Resource.ValueKind == JsonValueKind.String
            ? new IconSource.WindowsResourceName(Resource.GetString()!, Width, Height)
            : new IconSource.WindowsResource(Resource.Deserialize(TrayJsonContext.Default.UInt16), Width, Height);
    }
}

internal sealed record RemoteAboutMetadata
{
    public string? Name { get; init; }
    public string? Version { get; init; }
    public string? ShortVersion { get; init; }
    public string[]? Authors { get; init; }
    public string? Comments { get; init; }
    public string? Copyright { get; init; }
    public string? License { get; init; }
    public string? Website { get; init; }
    public string? WebsiteLabel { get; init; }
    public string? Credits { get; init; }
    public RemoteIcon? Icon { get; init; }

    internal AboutMetadata ToSdk() => new()
    {
        Name = Name,
        Version = Version,
        ShortVersion = ShortVersion,
        Authors = Authors,
        Comments = Comments,
        Copyright = Copyright,
        License = License,
        Website = Website,
        WebsiteLabel = WebsiteLabel,
        Credits = Credits,
        Icon = Icon?.ToSdk(),
    };
}
