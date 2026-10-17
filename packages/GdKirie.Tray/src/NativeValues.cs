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
        using var result = new Array();
        foreach (var item in items)
        {
            using var value = Variant.From(item.Native);
            result.Add(value);
        }

        return result;
    }

    internal static Variant Tray(TrayOptions options)
    {
        using var result = new Dictionary { ["iconAsTemplate"] = options.IconAsTemplate };
        Add(result, "id", options.Id);
        Add(result, "icon", options.Icon);
        if (options.Menu is not null)
        {
            using var menu = Variant.From(((TrayObject)options.Menu).Native);
            result["menu"] = menu;
        }

        Add(result, "tooltip", options.Tooltip);
        Add(result, "title", options.Title);
        Add(result, "menuOnLeftClick", options.MenuOnLeftClick);
        Add(result, "menuOnRightClick", options.MenuOnRightClick);
        Add(result, "guid", options.Guid);
        Add(result, "autosaveName", options.AutosaveName);
        Add(result, "tempDirPath", options.TempDirPath);
        return result;
    }

    internal static Variant Menu(MenuOptions options)
    {
        using var items = Items(options.Items);
        using var result = new Dictionary { ["items"] = items };
        Add(result, "id", options.Id);
        return result;
    }

    private static Dictionary Item(string? id, string text, bool enabled, Accelerator? accelerator = null)
    {
        var result = new Dictionary { ["text"] = text, ["enabled"] = enabled };
        Add(result, "id", id);
        if (accelerator is not null)
        {
            using var value = Accelerator(accelerator);
            result["accelerator"] = value;
        }

        return result;
    }

    internal static Variant Item(MenuItemOptions options)
    {
        using var result = Item(options.Id, options.Text, options.Enabled, options.Accelerator);
        return result;
    }

    internal static Variant Check(CheckMenuItemOptions options)
    {
        using var result = Item(options.Id, options.Text, options.Enabled, options.Accelerator);
        result["checked"] = options.Checked;
        return result;
    }

    internal static Variant IconItem(IconMenuItemOptions options)
    {
        using var result = Item(options.Id, options.Text, options.Enabled, options.Accelerator);
        Add(result, "icon", options.Icon);
        result["iconAsTemplate"] = options.IconAsTemplate;
        return result;
    }

    internal static Variant Submenu(SubmenuOptions options)
    {
        using var result = Item(options.Id, options.Text, options.Enabled);
        using var items = Items(options.Items);
        result["items"] = items;
        Add(result, "icon", options.Icon);
        result["iconAsTemplate"] = options.IconAsTemplate;
        return result;
    }

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
