param(
    [string]$RimWorldPath = "",
    [string]$HarmonyDll = ""
)

$ErrorActionPreference = "Stop"
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModRoot = Split-Path -Parent $Here
$OutDir = Join-Path $ModRoot "1.6\Assemblies"
$Source = Join-Path $Here "GrowthVatAcceleratorXPCompat.cs"
$Output = Join-Path $OutDir "GrowthVatAcceleratorXPCompat.dll"

if ([string]::IsNullOrWhiteSpace($RimWorldPath)) {
    $candidates = @(
        "$env:ProgramFiles(x86)\Steam\steamapps\common\RimWorld",
        "$env:ProgramFiles\Steam\steamapps\common\RimWorld"
    )
    $RimWorldPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $RimWorldPath -or -not (Test-Path $RimWorldPath)) {
    throw "RimWorld folder not found. Run: .\Build.ps1 -RimWorldPath 'D:\SteamLibrary\steamapps\common\RimWorld'"
}

$Managed = Join-Path $RimWorldPath "RimWorldWin64_Data\Managed"
$AssemblyCSharp = Join-Path $Managed "Assembly-CSharp.dll"
$UnityCore = Join-Path $Managed "UnityEngine.CoreModule.dll"
$UnityLegacy = Join-Path $Managed "UnityEngine.dll"
$UnityText = Join-Path $Managed "UnityEngine.TextRenderingModule.dll"
$UnityIMGUI = Join-Path $Managed "UnityEngine.IMGUIModule.dll"
$NetStandard = Join-Path $Managed "netstandard.dll"
$SystemRuntime = Join-Path $Managed "System.Runtime.dll"

if ([string]::IsNullOrWhiteSpace($HarmonyDll)) {
    $workshopRoots = @(
        "$env:ProgramFiles(x86)\Steam\steamapps\workshop\content\294100\2009463077",
        "$env:ProgramFiles\Steam\steamapps\workshop\content\294100\2009463077"
    )
    $HarmonyDll = Get-ChildItem $workshopRoots -Filter "0Harmony.dll" -Recurse -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty FullName -First 1
}
if (-not $HarmonyDll -or -not (Test-Path $HarmonyDll)) {
    throw "0Harmony.dll not found. Pass -HarmonyDll with the full path to Harmony's DLL."
}

$cscCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$csc = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { throw "C# compiler csc.exe not found." }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$refs = @($AssemblyCSharp, $HarmonyDll)
if (Test-Path $NetStandard) { $refs += $NetStandard }
else { throw "RimWorld netstandard.dll not found at: $NetStandard" }
if (Test-Path $SystemRuntime) { $refs += $SystemRuntime }
if (Test-Path $UnityCore) { $refs += $UnityCore }
elseif (Test-Path $UnityLegacy) { $refs += $UnityLegacy }
if (Test-Path $UnityText) { $refs += $UnityText }
else { throw "UnityEngine.TextRenderingModule.dll not found at: $UnityText" }
if (Test-Path $UnityIMGUI) { $refs += $UnityIMGUI }
else { throw "UnityEngine.IMGUIModule.dll not found at: $UnityIMGUI" }

$args = @("/nologo", "/target:library", "/optimize+", "/langversion:5", "/out:$Output")
$args += $refs | ForEach-Object { "/reference:$_" }
$args += $Source

& $csc @args
if ($LASTEXITCODE -ne 0) { throw "Compilation failed." }
Write-Host "Built: $Output" -ForegroundColor Green
