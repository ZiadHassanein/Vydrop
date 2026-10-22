$ErrorActionPreference='Stop'
# This is runner setup, never an app/user-desktop preference.
if($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_OS -ne 'Windows') { return }
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class VydropCiDisplay {
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct Mode {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string DeviceName;
        public ushort SpecVersion, DriverVersion, Size, DriverExtra;
        public uint Fields;
        public int PositionX, PositionY;
        public uint Orientation, FixedOutput;
        public short Color, Duplex, YResolution, TTOption, Collate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string FormName;
        public ushort LogPixels;
        public uint BitsPerPel, Width, Height, Flags, Frequency, IcmMethod, IcmIntent, MediaType, DitherType, Reserved1, Reserved2, PanningWidth, PanningHeight;
    }
    [DllImport("user32.dll", EntryPoint="EnumDisplaySettingsW", CharSet=CharSet.Unicode)]
    [return:MarshalAs(UnmanagedType.Bool)] public static extern bool Read(string device, int number, ref Mode mode);
    [DllImport("user32.dll", EntryPoint="ChangeDisplaySettingsW", CharSet=CharSet.Unicode)]
    public static extern int Change(ref Mode mode, uint flags);
    public static Mode Current() {
        var mode = new Mode { Size=(ushort)Marshal.SizeOf<Mode>() };
        if(!Read(null,-1,ref mode)) throw new InvalidOperationException("Cannot read the runner display mode.");
        return mode;
    }
}
'@
$ciDisplay=[VydropCiDisplay]::Current()
Write-Output "Runner display: $($ciDisplay.Width)x$($ciDisplay.Height)"
if($ciDisplay.Width -ge 1440 -and $ciDisplay.Height -ge 900) { return }
foreach($ciCandidate in @(@(1920,1080),@(1600,900),@(1440,900),@(1280,1024))) {
    $ciDisplay=[VydropCiDisplay]::Current()
    $ciDisplay.Width=$ciCandidate[0]; $ciDisplay.Height=$ciCandidate[1]; $ciDisplay.Fields=0x180000
    if([VydropCiDisplay]::Change([ref]$ciDisplay,2) -ne 0) { continue }
    if([VydropCiDisplay]::Change([ref]$ciDisplay,0) -ne 0) { continue }
    Start-Sleep -Milliseconds 300
    $ciActual=[VydropCiDisplay]::Current()
    if($ciActual.Width -ge 1280 -and $ciActual.Height -ge 800) {
        Write-Output "Native UI tests use $($ciActual.Width)x$($ciActual.Height); compact windows remain covered separately."
        return
    }
}
throw 'The hosted display cannot fit the wide native UI regression cases.'
