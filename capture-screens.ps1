# Captures the running overlay windows as screenshots into docs\screenshots.
Add-Type -AssemblyName System.Drawing

$cs = @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WEnum {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lp);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lp);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@
Add-Type -TypeDefinition $cs

[WEnum]::SetProcessDPIAware() | Out-Null

$proc = Get-Process Aion2RiftOverlay -ErrorAction Stop
$targetId = [uint32]$proc.Id

$map = @{
    'AION 2 Space Rift Overlay' = 'spacerift.png'
    'DAILY RESET' = 'daily-reset.png'
    'WEEKLY RESET' = 'weekly-reset.png'
}

$outDir = 'I:\Aion2RiftOverlay\docs\screenshots'
New-Item -ItemType Directory -Force $outDir | Out-Null

$callback = [WEnum+EnumWindowsProc]{
    param($hWnd, $lp)
    [uint32]$wpid = 0
    [WEnum]::GetWindowThreadProcessId($hWnd, [ref]$wpid) | Out-Null
    if ($wpid -ne $targetId) { return $true }

    $sb = New-Object System.Text.StringBuilder 128
    [WEnum]::GetWindowText($hWnd, $sb, 128) | Out-Null
    $title = $sb.ToString()
    if (-not $map.ContainsKey($title)) { return $true }

    $rect = New-Object WEnum+RECT
    [WEnum]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { return $true }

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $g.Dispose()

    $path = Join-Path $outDir $map[$title]
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "saved $($map[$title]) ($w x $h)"
    return $true
}
[WEnum]::EnumWindows($callback, [IntPtr]::Zero) | Out-Null
Write-Output 'done'