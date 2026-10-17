using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Eventa;

namespace GdKirie.Tray.EventaAdapter;

/// <summary>Owns browser bindings and borrows an application's Eventa context.</summary>
public sealed class TrayEventaAdapter : IDisposable
{
    private static long _nextHandle;
    private readonly IEventContext _context;
    private readonly Dictionary<string, TrayObject> _handles = new(StringComparer.Ordinal);
    private readonly List<IDisposable> _registrations = [];
    private bool _disposed;

    internal TrayEventaAdapter(IEventContext context)
    {
        _context = context;
        try
        {
            RegisterMethods();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Returns the object's live browser descriptor, retaining an independent reference when first exposed.</summary>
    public TrayResource Expose(TrayObject item)
    {
        EnsureAvailable();
        ArgumentNullException.ThrowIfNull(item);
        return Bind(item.Retain());
    }

    /// <summary>Detaches browser methods and releases every browser reference.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        TrayObject.EnsureMainThread();
        _disposed = true;
        foreach (var registration in _registrations)
        {
            registration.Dispose();
        }

        _registrations.Clear();
        foreach (var item in _handles.Values)
        {
            item.Dispose();
        }

        _handles.Clear();
    }

    private void RegisterMethods()
    {
        var json = TrayJsonContext.Default;
        Register(TrayMethod.ResourceClose, json.HandleRequest,
            request => Close(request.Handle));

        Register(TrayMethod.TrayCreate, json.TrayCreateRequest, request =>
        {
            var options = request.Options;
            return Bind(new TrayIcon(new TrayOptions
            {
                Id = options.Id,
                Icon = options.Icon?.ToSdk(),
                IconAsTemplate = options.IconAsTemplate,
                Menu = Menu(options.Menu),
                Tooltip = options.Tooltip,
                Title = options.Title,
                MenuOnLeftClick = options.MenuOnLeftClick,
                MenuOnRightClick = options.MenuOnRightClick,
                Guid = options.Guid,
                AutosaveName = options.AutosaveName,
                TempDirPath = options.TempDirPath,
            }));
        });
        Register(TrayMethod.MenuCreate, json.MenuCreateRequest, request =>
            Bind(new Menu(new MenuOptions { Id = request.Options.Id, Items = Items(request.Options.Items) })));
        Register(TrayMethod.SubmenuCreate, json.SubmenuCreateRequest, request =>
        {
            var options = request.Options;
            return Bind(new Submenu(new SubmenuOptions(options.Text)
            {
                Id = options.Id,
                Enabled = options.Enabled,
                Items = Items(options.Items),
                Icon = options.Icon?.ToSdk(),
                IconAsTemplate = options.IconAsTemplate,
            }));
        });
        Register(TrayMethod.ItemCreate, json.ItemCreateRequest, request =>
        {
            var options = request.Options;
            return Bind(new MenuItem(new MenuItemOptions(options.Text)
            {
                Id = options.Id,
                Enabled = options.Enabled,
                Accelerator = options.Accelerator?.ToSdk(),
            }));
        });
        Register(TrayMethod.CheckCreate, json.CheckCreateRequest, request =>
        {
            var options = request.Options;
            return Bind(new CheckMenuItem(new CheckMenuItemOptions(options.Text)
            {
                Id = options.Id,
                Enabled = options.Enabled,
                Checked = options.Checked,
                Accelerator = options.Accelerator?.ToSdk(),
            }));
        });
        Register(TrayMethod.IconCreate, json.IconCreateRequest, request =>
        {
            var options = request.Options;
            return Bind(new IconMenuItem(new IconMenuItemOptions(options.Text)
            {
                Id = options.Id,
                Enabled = options.Enabled,
                Accelerator = options.Accelerator?.ToSdk(),
                Icon = options.Icon?.ToSdk(),
                IconAsTemplate = options.IconAsTemplate,
            }));
        });
        Register(TrayMethod.PredefinedSeparator, json.EmptyRequest,
            _ => Bind(PredefinedMenuItem.Separator()));
        Register(TrayMethod.PredefinedSectionHeader, json.PredefinedSectionHeaderRequest,
            request => Bind(PredefinedMenuItem.SectionHeader(request.Text)));
        Register(TrayMethod.PredefinedCopy, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Copy(request.Text)));
        Register(TrayMethod.PredefinedCut, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Cut(request.Text)));
        Register(TrayMethod.PredefinedPaste, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Paste(request.Text)));
        Register(TrayMethod.PredefinedPasteAndMatchStyle, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.PasteAndMatchStyle(request.Text)));
        Register(TrayMethod.PredefinedDelete, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Delete(request.Text)));
        Register(TrayMethod.PredefinedSelectAll, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.SelectAll(request.Text)));
        Register(TrayMethod.PredefinedUndo, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Undo(request.Text)));
        Register(TrayMethod.PredefinedRedo, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Redo(request.Text)));
        Register(TrayMethod.PredefinedMinimize, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Minimize(request.Text)));
        Register(TrayMethod.PredefinedMaximize, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Maximize(request.Text)));
        Register(TrayMethod.PredefinedZoom, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Zoom(request.Text)));
        Register(TrayMethod.PredefinedActualSize, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.ActualSize(request.Text)));
        Register(TrayMethod.PredefinedZoomIn, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.ZoomIn(request.Text)));
        Register(TrayMethod.PredefinedZoomOut, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.ZoomOut(request.Text)));
        Register(TrayMethod.PredefinedFullscreen, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Fullscreen(request.Text)));
        Register(TrayMethod.PredefinedHide, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Hide(request.Text)));
        Register(TrayMethod.PredefinedHideOthers, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.HideOthers(request.Text)));
        Register(TrayMethod.PredefinedShowAll, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.ShowAll(request.Text)));
        Register(TrayMethod.PredefinedCloseWindow, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.CloseWindow(request.Text)));
        Register(TrayMethod.PredefinedQuit, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Quit(request.Text)));
        Register(TrayMethod.PredefinedServices, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.Services(request.Text)));
        Register(TrayMethod.PredefinedBringAllToFront, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.BringAllToFront(request.Text)));
        Register(TrayMethod.PredefinedStartSpeaking, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.StartSpeaking(request.Text)));
        Register(TrayMethod.PredefinedStopSpeaking, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.StopSpeaking(request.Text)));
        Register(TrayMethod.PredefinedStartDictation, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.StartDictation(request.Text)));
        Register(TrayMethod.PredefinedEmojiAndSymbols, json.PredefinedTextRequest,
            request => Bind(PredefinedMenuItem.EmojiAndSymbols(request.Text)));
        Register(TrayMethod.PredefinedAbout, json.PredefinedAboutRequest,
            request => Bind(PredefinedMenuItem.About(request.Text, request.Metadata?.ToSdk())));

        Register(TrayMethod.TraySetIcon, json.IconRequest,
            request => Get<TrayIcon>(request.Handle).SetIcon(request.Icon?.ToSdk(), request.IconAsTemplate));
        Register(TrayMethod.TraySetMenu, json.MenuRequest,
            request => Get<TrayIcon>(request.Handle).SetMenu(Menu(request.Menu)));
        Register(TrayMethod.TraySetTooltip, json.TooltipRequest,
            request => Get<TrayIcon>(request.Handle).SetTooltip(request.Tooltip));
        Register(TrayMethod.TraySetTitle, json.TitleRequest,
            request => Get<TrayIcon>(request.Handle).SetTitle(request.Title));
        Register(TrayMethod.TraySetVisible, json.VisibleRequest,
            request => Get<TrayIcon>(request.Handle).SetVisible(request.Visible));
        Register(TrayMethod.TraySetMenuOnLeftClick, json.EnabledRequest,
            request => Get<TrayIcon>(request.Handle).SetMenuOnLeftClick(request.Enabled));
        Register(TrayMethod.TraySetMenuOnRightClick, json.EnabledRequest,
            request => Get<TrayIcon>(request.Handle).SetMenuOnRightClick(request.Enabled));
        Register(TrayMethod.TrayShowMenu, json.HandleRequest,
            request => Get<TrayIcon>(request.Handle).ShowMenu());
        Register(TrayMethod.TrayRect, json.HandleRequest,
            request => Get<TrayIcon>(request.Handle).Rect() is { } rect ? ToRemote(rect) : null);
        Register(TrayMethod.TrayIconIsTemplate, json.HandleRequest,
            request => Get<TrayIcon>(request.Handle).IconIsTemplate());
        Register(TrayMethod.TraySetTempDirPath, json.PathRequest,
            request => Get<TrayIcon>(request.Handle).SetTempDirPath(request.Path));

        Register(TrayMethod.MenuItems, json.HandleRequest, request =>
            Get<IMenuContainer>(request.Handle).Items.Select(Bind).ToArray());
        Register(TrayMethod.MenuAppend, json.ItemsRequest,
            request => Get<IMenuContainer>(request.Handle).Append(Items(request.Items)));
        Register(TrayMethod.MenuPrepend, json.ItemsRequest,
            request => Get<IMenuContainer>(request.Handle).Prepend(Items(request.Items)));
        Register(TrayMethod.MenuInsert, json.InsertRequest,
            request => Get<IMenuContainer>(request.Handle).Insert(request.Position, Items(request.Items)));
        Register(TrayMethod.MenuRemove, json.RemoveRequest,
            request => Get<IMenuContainer>(request.Handle).Remove(Get<MenuEntry>(request.Item)));
        Register(TrayMethod.MenuRemoveAt, json.PositionRequest, request =>
            Get<IMenuContainer>(request.Handle).RemoveAt(request.Position) is { } item
                ? Bind(item) : null);

        Register(TrayMethod.ItemText, json.HandleRequest,
            request => Get<MenuEntry>(request.Handle).Text);
        Register(TrayMethod.ItemSetText, json.TextRequest,
            request =>
            {
                Get<MenuEntry>(request.Handle).Text = request.Text;
            });
        Register(TrayMethod.ItemIsEnabled, json.HandleRequest,
            request => Get<EnabledMenuEntry>(request.Handle).Enabled);
        Register(TrayMethod.ItemSetEnabled, json.EnabledRequest,
            request =>
            {
                Get<EnabledMenuEntry>(request.Handle).Enabled = request.Enabled;
            });
        Register(TrayMethod.CheckIsChecked, json.HandleRequest,
            request => Get<CheckMenuItem>(request.Handle).Checked);
        Register(TrayMethod.CheckSetChecked, json.CheckedRequest,
            request =>
            {
                Get<CheckMenuItem>(request.Handle).Checked = request.Checked;
            });
        Register(TrayMethod.ItemSetAccelerator, json.AcceleratorRequest,
            request => SetAccelerator(Get<MenuEntry>(request.Handle), request.Accelerator?.ToSdk()));
        Register(TrayMethod.ItemSetIcon, json.IconRequest,
            request => SetIcon(Get<MenuEntry>(request.Handle), request.Icon?.ToSdk(), request.IconAsTemplate));
        Register(TrayMethod.ItemIconIsTemplate, json.HandleRequest,
            request => IconIsTemplate(Get<MenuEntry>(request.Handle)));
    }

    private TrayResource Bind(TrayObject item)
    {
        try
        {
            foreach (var (existingHandle, existing) in _handles)
            {
                if (existing.InstanceId == item.InstanceId)
                {
                    item.Dispose();
                    return new TrayResource(existingHandle, existing.Kind, existing.Id);
                }
            }

            var handle = $"tray-{++_nextHandle}";
            var resource = new TrayResource(handle, item.Kind, item.Id);
            if (item is MenuEntry entry)
            {
                entry.Clicked += (_, _) => Emit(new RemoteEvent("menu", Item: resource));
            }

            if (item is TrayIcon tray)
            {
                tray.Event += (_, args) => EmitTrayEvent(resource, args);
            }

            _handles.Add(handle, item);
            return resource;
        }
        catch
        {
            item.Dispose();
            throw;
        }
    }

    private void Close(string handle)
    {
        var item = GetObject(handle);
        _handles.Remove(handle);
        item.Dispose();
    }

    private TrayObject GetObject(string handle) =>
        _handles.GetValueOrDefault(handle)
            ?? throw new TrayException("invalid-handle", $"Unknown browser tray handle: {handle}");

    private T Get<T>(string handle) where T : class =>
        GetObject(handle) as T
            ?? throw new TrayException("invalid-argument", $"Browser tray handle '{handle}' is not a {typeof(T).Name}.");

    private IMenuContainer? Menu(string? handle) => handle is null ? null : Get<IMenuContainer>(handle);

    private MenuEntry[] Items(string[] handles) => handles.Select(Get<MenuEntry>).ToArray();

    private void Register<T>(
        TrayMethod method,
        JsonTypeInfo<T> requestType,
        Action<T> handler) where T : class =>
        Register(method, requestType, request =>
        {
            handler(request);
            return null;
        });

    private void Register<T>(
        TrayMethod method,
        JsonTypeInfo<T> requestType,
        Func<T, object?> handler) where T : class
    {
        _registrations.Add(_context.RegisterInvokeHandler(TrayEvents.Methods[method], (body, _) =>
        {
            EnsureAvailable();
            JsonElement reply;
            try
            {
                var request = body.Deserialize(requestType) ?? throw new JsonException("A tray request must be an object.");
                reply = JsonSerializer.SerializeToElement(new SuccessReply(handler(request)), TrayJsonContext.Default.SuccessReply);
            }
            catch (TrayException exception)
            {
                reply = Failure(exception.Code, exception.Message);
            }
            catch (JsonException exception)
            {
                reply = Failure("invalid-argument", exception.Message);
            }
            catch (ArgumentException exception)
            {
                reply = Failure("invalid-argument", exception.Message);
            }

            return Task.FromResult(reply);
        }));
    }

    private static JsonElement Failure(string code, string message) =>
        JsonSerializer.SerializeToElement(new FailureReply(new RemoteError(code, message)), TrayJsonContext.Default.FailureReply);

    private void Emit(RemoteEvent value) => _context.Emit(TrayEvents.Event, JsonSerializer.SerializeToElement(value, TrayJsonContext.Default.RemoteEvent));

    private void EnsureAvailable()
    {
        TrayObject.EnsureMainThread();
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void EmitTrayEvent(TrayResource resource, TrayIconEvent args)
    {
        var type = JsonNamingPolicy.KebabCaseLower.ConvertName(args.Type.ToString());
        Emit(new RemoteEvent(type, Tray: resource,
            Position: args.Position, Rect: ToRemote(args.Rect),
            Button: args.Button?.ToString().ToLowerInvariant(), ButtonState: args.ButtonState?.ToString().ToLowerInvariant()));
    }

    private static RemoteRect ToRemote(TrayRect rect) => new(rect.Position, new RemoteSize(rect.Width, rect.Height));
    private static void SetAccelerator(MenuEntry item, Accelerator? accelerator)
    {
        switch (item)
        {
            case MenuItem plain:
                plain.SetAccelerator(accelerator);
                return;

            case CheckMenuItem check:
                check.SetAccelerator(accelerator);
                return;

            case IconMenuItem icon:
                icon.SetAccelerator(accelerator);
                return;

            default:
                throw new TrayException("unsupported", $"{item.Kind} does not support accelerators.");
        }
    }

    private static void SetIcon(MenuEntry item, IconSource? source, bool template)
    {
        switch (item)
        {
            case IconMenuItem icon:
                icon.SetIcon(source, template);
                return;

            case Submenu submenu:
                submenu.SetIcon(source, template);
                return;

            default:
                throw new TrayException("unsupported", $"{item.Kind} does not support icons.");
        }
    }

    private static bool IconIsTemplate(MenuEntry item) => item switch
    {
        IconMenuItem icon => icon.IconIsTemplate(),
        Submenu submenu => submenu.IconIsTemplate(),
        _ => throw new TrayException("unsupported", $"{item.Kind} does not support icon template queries."),
    };

}
