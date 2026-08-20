param(
    [Parameter(Mandatory = $true)][int]$ProcessId,
    [string]$ExpectedExecutable,
    [switch]$CloseWindow
)

$ErrorActionPreference = 'Stop'

if (-not ('ForjaIdentity.Native' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ForjaIdentity
{
    public static class Native
    {
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey { public Guid FormatId; public uint PropertyId; }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort ValueType;
            [FieldOffset(8)] public IntPtr PointerValue;
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint count);
            [PreserveSig] int GetAt(uint index, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
            [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
            [PreserveSig] int Commit();
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(IntPtr hwnd, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore store);
        [DllImport("ole32.dll")] private static extern int PropVariantClear(ref PropVariant value);
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);

        public static string GetAppId(IntPtr hwnd)
        {
            Guid iid = typeof(IPropertyStore).GUID;
            IPropertyStore store;
            if (SHGetPropertyStoreForWindow(hwnd, ref iid, out store) != 0) return string.Empty;
            var key = new PropertyKey { FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), PropertyId = 5 };
            try
            {
                PropVariant value;
                if (store.GetValue(ref key, out value) != 0) return string.Empty;
                try { return value.PointerValue == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(value.PointerValue) ?? string.Empty; }
                finally { PropVariantClear(ref value); }
            }
            finally { Marshal.ReleaseComObject(store); }
        }

        public static long GetBigIcon(IntPtr hwnd) { return SendMessage(hwnd, 0x007F, new IntPtr(1), IntPtr.Zero).ToInt64(); }
        public static long GetSmallIcon(IntPtr hwnd) { return SendMessage(hwnd, 0x007F, IntPtr.Zero, IntPtr.Zero).ToInt64(); }
        public static void Close(IntPtr hwnd) { PostMessage(hwnd, 0x0010, IntPtr.Zero, IntPtr.Zero); }

        public static string[] VisibleWindows(int processId)
        {
            var windows = new System.Collections.Generic.List<string>();
            EnumWindows(delegate(IntPtr hwnd, IntPtr lParam)
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid == processId && IsWindowVisible(hwnd))
                {
                    var title = new StringBuilder(512);
                    GetWindowText(hwnd, title, title.Capacity);
                    windows.Add(hwnd.ToInt64().ToString() + "|" + title.ToString());
                }
                return true;
            }, IntPtr.Zero);
            return windows.ToArray();
        }
    }
}
'@
}

$process = Get-Process -Id $ProcessId -ErrorAction Stop
$deadline = [DateTime]::UtcNow.AddSeconds(8)
while ($process.MainWindowHandle -eq 0 -and [DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Milliseconds 150
    $process.Refresh()
}

$handle = $process.MainWindowHandle
$appId = if ($handle -ne 0) { [ForjaIdentity.Native]::GetAppId($handle) } else { '' }
$bigIcon = if ($handle -ne 0) { [ForjaIdentity.Native]::GetBigIcon($handle) } else { 0 }
$smallIcon = if ($handle -ne 0) { [ForjaIdentity.Native]::GetSmallIcon($handle) } else { 0 }
$windows = [ForjaIdentity.Native]::VisibleWindows($ProcessId)
$expectedExe = if ([string]::IsNullOrWhiteSpace($ExpectedExecutable)) {
    Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) 'Programs\Forja de Cuadros\ForjaDeCuadros.exe'
} else {
    [System.IO.Path]::GetFullPath($ExpectedExecutable)
}
$checks = [ordered]@{
    Executable = ($process.Path -eq $expectedExe)
    WindowHandle = ($handle -ne 0)
    WindowTitle = ($process.MainWindowTitle -eq 'Forja de Cuadros')
    BigIcon = ($bigIcon -ne 0)
    SmallIcon = ($smallIcon -ne 0)
    SingleVisibleWindow = ($windows.Count -eq 1)
}

Write-Output "PID=$ProcessId"
Write-Output "EXE=$($process.Path)"
Write-Output "TITLE=$($process.MainWindowTitle)"
Write-Output "HWND=$handle"
Write-Output "APP_ID=$appId"
Write-Output "BIG_ICON=$bigIcon"
Write-Output "SMALL_ICON=$smallIcon"
Write-Output "VISIBLE_WINDOWS=$($windows -join ';')"
$checks.GetEnumerator() | ForEach-Object { Write-Output "CHECK_$($_.Key)=$($_.Value)" }

if ($CloseWindow -and $handle -ne 0) {
    [ForjaIdentity.Native]::Close($handle)
    if (-not $process.WaitForExit(8000)) { throw 'La ventana no cerro normalmente dentro de 8 segundos.' }
    Write-Output 'NORMAL_CLOSE=True'
}

if ($checks.Values -contains $false) { exit 1 }
