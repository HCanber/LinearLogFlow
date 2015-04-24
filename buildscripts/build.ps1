param($task = "default")

function Get-PowershellBits(){
  Switch ([IntPtr]::Size) {
    4 { Return "32-bit"}
    8 { Return "64-bit"}
    default { Return null }
  }
}

$scriptPath = $MyInvocation.MyCommand.Path
$scriptDir = Split-Path $scriptPath
$srcDir = Resolve-Path (Join-Path $scriptDir ..\src)
$packagesDir = Join-Path $srcDir packages
$nuget_exe="$scriptDir\nuget.exe"

if(!(Test-Path $nuget_exe)) {
  Write-Host "Downloading nuget.exe"
  Invoke-WebRequest https://nuget.org/nuget.exe -OutFile $nuget_exe
}

#Write-Host '$scriptPath='$scriptPath
#Write-Host '$scriptDir='$scriptDir
#Write-Host '$srcDir='$srcDir
#Write-Host '$packagesDir='$packagesDir
#Write-Host '$nuget_exe='$nuget_exe

get-module psake | remove-module

# Download all nuget packages needed. They are downloaded to src\packages dir
$env:EnableNuGetPackageRestore="true"
try {
  & $nuget_exe install $scriptDir\packages.config -OutputDirectory $packagesDir 2>&1
} catch [Exception]{
  Write-Host "Unable to execute`n$nuget_exe install $scriptDir\packages.config -OutputDirectory $packagesDir. $($_.Exception.Message)"
  exit 1
}
if ($lastexitcode -ne 0) {
  Write-Host "An error occured while executing`n$nuget_exe install $scriptDir\packages.config -OutputDirectory $packagesDir"
  exit 1
}

# Import the psake module
$psakeModulePath = (Get-ChildItem "$packagesDir\psake.*\tools\psake.psm1" | Select-Object -First 1)
import-module $psakeModulePath

$global:build_defaults=@{
    nuget_exe=$nuget_exe;
    nuget_packages_dir=$packagesDir;
}

Write-Host ("Powershell {0} {1}" -f [string]$psversiontable.psversion, (Get-PowershellBits))

# Run psake with our own build script
invoke-psake "$scriptDir\psakeBuildScript.ps1" $task
