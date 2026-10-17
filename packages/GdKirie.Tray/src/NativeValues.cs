using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace GdKirie.Tray;

internal static class NativeValues
{
    internal static Variant Icon(IconSource source)
    {
        using var result = new Dictionary();
        switch (source)
        {
            case IconSource.Resource icon:
                result["kind"] = "resource";
                result["path"] = icon.Path;
                break;
            case IconSource.Rgba icon:
                result["kind"] = "rgba";
                result["rgba"] = icon.Pixels;
                result["width"] = icon.Width;
                result["height"] = icon.Height;
                break;
            case IconSource.Native icon:
                result["kind"] = "native";
                result["name"] = icon.Name;
                break;
            case IconSource.NativeName icon:
                result["kind"] = "native-name";
                result["name"] = icon.Name;
                break;
            case IconSource.WindowsStock icon:
                result["kind"] = "windows-stock";
                result["id"] = icon.Id;
                break;
            case IconSource.WindowsPath icon:
                result["kind"] = "windows-path";
                result["path"] = icon.Path;
                Add(result, "width", icon.Width);
                Add(result, "height", icon.Height);
                break;
            case IconSource.WindowsResource icon:
                result["kind"] = "windows-resource";
                result["resource"] = icon.Ordinal;
                Add(result, "width", icon.Width);
                Add(result, "height", icon.Height);
                break;
            case IconSource.WindowsResourceName icon:
                result["kind"] = "windows-resource";
                result["resource"] = icon.Name;
                Add(result, "width", icon.Width);
                Add(result, "height", icon.Height);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(source));
        }

        return result;
    }

    internal static Variant NullableIcon(IconSource? icon) => icon is null ? default : Icon(icon);

    internal static Variant Accelerator(Accelerator? accelerator)
    {
        if (accelerator is null)
        {
            return default;
        }

        using var result = new Dictionary
        {
            ["kind"] = accelerator is GdKirie.Tray.Accelerator.Physical ? "physical" : "logical",
            ["value"] = accelerator.Value,
        };
        return result;
    }

    internal static Variant Items(IEnumerable<MenuEntry> items)
    {
        TrayObject.EnsureMainThread();
        // The native factory supplies Array[KirieTrayItem], which C# cannot express as Array<T>.
        using var value = ClassDB.ClassCallStatic("KirieTrayItem", "item_array");
        using var result = value.AsGodotArray();
        foreach (var item in items)
        {
            using var entry = Variant.From(item.Native);
            result.Add(entry);
        }

        return result;
    }

    private static Variant NullableString(string? value) => value is null ? default(Variant) : value;

    internal static Variant[] Tray(TrayOptions options)
    {
        using var values = new Dictionary();
        Add(values, "id", options.Id);
        Add(values, "tooltip", options.Tooltip);
        Add(values, "title", options.Title);
        Add(values, "menuOnLeftClick", options.MenuOnLeftClick);
        Add(values, "menuOnRightClick", options.MenuOnRightClick);
        Add(values, "guid", options.Guid);
        Add(values, "autosaveName", options.AutosaveName);
        Add(values, "tempDirPath", options.TempDirPath);
        return
        [
            NullableIcon(options.Icon), options.IconAsTemplate,
            options.Menu is null ? default(Variant) : ((TrayObject)options.Menu).Native, values,
        ];
    }

    internal static Variant[] Menu(MenuOptions options) =>
        [NullableString(options.Id), Items(options.Items)];

    internal static Variant[] Item(MenuItemOptions options) =>
        [NullableString(options.Id), options.Text, options.Enabled, Accelerator(options.Accelerator)];

    internal static Variant[] Check(CheckMenuItemOptions options) =>
        [NullableString(options.Id), options.Text, options.Enabled, options.Checked, Accelerator(options.Accelerator)];

    internal static Variant[] IconItem(IconMenuItemOptions options) =>
    [
        NullableString(options.Id), options.Text, options.Enabled, Accelerator(options.Accelerator),
        NullableIcon(options.Icon), options.IconAsTemplate,
    ];

    internal static Variant[] Submenu(SubmenuOptions options) =>
    [
        NullableString(options.Id), options.Text, options.Enabled, Items(options.Items),
        NullableIcon(options.Icon), options.IconAsTemplate,
    ];

    internal static Variant Metadata(AboutMetadata metadata)
    {
        using var result = new Dictionary();
        Add(result, "name", metadata.Name);
        Add(result, "version", metadata.Version);
        Add(result, "shortVersion", metadata.ShortVersion);
        if (metadata.Authors is not null)
        {
            using var authors = new Array();
            foreach (var author in metadata.Authors)
            {
                using var value = Variant.From(author);
                authors.Add(value);
            }

            result["authors"] = authors;
        }

        Add(result, "comments", metadata.Comments);
        Add(result, "copyright", metadata.Copyright);
        Add(result, "license", metadata.License);
        Add(result, "website", metadata.Website);
        Add(result, "websiteLabel", metadata.WebsiteLabel);
        Add(result, "credits", metadata.Credits);
        Add(result, "icon", metadata.Icon);
        return result;
    }

    private static void Add(Dictionary result, string key, string? value)
    {
        if (value is not null)
        {
            result[key] = value;
        }
    }

    private static void Add(Dictionary result, string key, bool? value)
    {
        if (value.HasValue)
        {
            result[key] = value.Value;
        }
    }

    private static void Add(Dictionary result, string key, uint? value)
    {
        if (value.HasValue)
        {
            result[key] = value.Value;
        }
    }

    private static void Add(Dictionary result, string key, IconSource? value)
    {
        if (value is not null)
        {
            using var icon = Icon(value);
            result[key] = icon;
        }
    }
}
