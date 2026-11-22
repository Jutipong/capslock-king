using System.IO;
using System.Diagnostics;

namespace OpenKey.Core;

/// <summary>log แบบเบา ๆ: เขียน openkey.log ข้าง exe เมื่อเปิด --debug</summary>
public static class Log
{
    private const long MaxBytes = 1_000_000;
    private static readonly object Gate = new();
    private static string? _path;
    private static bool _enabled;

    public static void Init(bool enabled, string path)
    {
        _enabled = enabled;
        if (!enabled)
        {
            return;
        }

        try
        {
            if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
            {
                File.Delete(path);
            }
            _path = path;
        }
        catch
        {
            _path = null;
        }
    }

    public static void Info(string message)
    {
        Debug.WriteLine($"[OpenKey] {message}");
        if (!_enabled || _path is null)
        {
            return;
        }

        lock (Gate)
        {
            try
            {
                File.AppendAllText(_path, $"[{DateTime.Now:MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // log พังไม่ควรทำให้โปรแกรมตาย
            }
        }
    }
}
