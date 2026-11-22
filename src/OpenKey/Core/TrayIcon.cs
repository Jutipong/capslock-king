using System.Drawing;
using System.Windows.Forms;

namespace OpenKey.Core;

public enum TrayCommand
{
    Settings,
    Reload,
    OpenConfig,
    OpenFolder,
    Exit,
}

/// <summary>tray icon + popup menu (ใช้ WinForms NotifyIcon — ไม่ต้องพึ่ง library เสริม)</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;

    public TrayIcon(Action<TrayCommand> onCommand)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings…", null, (_, _) => onCommand(TrayCommand.Settings));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Reload config", null, (_, _) => onCommand(TrayCommand.Reload));
        menu.Items.Add("Open config.json", null, (_, _) => onCommand(TrayCommand.OpenConfig));
        menu.Items.Add("Open config folder", null, (_, _) => onCommand(TrayCommand.OpenFolder));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => onCommand(TrayCommand.Exit));

        _icon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "OpenKey",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => onCommand(TrayCommand.Settings);
    }

    private static Icon LoadIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (path is not null)
            {
                var icon = Icon.ExtractAssociatedIcon(path);
                if (icon is not null)
                {
                    return icon;
                }
            }
        }
        catch
        {
            // ใช้ไอคอนระบบแทน
        }
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
