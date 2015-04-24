Properties {
  $build_dir = Split-Path $psake.build_script_file

  $root_dir = Normalize-Path "$build_dir\.."
  $src_dir =  "$root_dir\src"

  $sln_path =  FindFile "$src_dir" *.sln
  $configuration = "Release"

  $packages_dir = "$src_dir\packages"
  $release_dir = "$root_dir\_release"
  $nuget_exe=$global:build_defaults["nuget_exe"]
}


Task default -Depends Release

Task Release -Depends Clean, PrepareRelease, Compile, RemoveUnnecessaryFiles {
  Write-Info "Release files: $release_dir"
}

Task Compile -Depends Clean {
  Write-Info "Building $sln_path"
  Exec { msbuild $sln_path /p:Configuration=$configuration /v:quiet /p:OutDir=$release_dir }
}

Task RemoveUnnecessaryFiles {
  del "$release_dir\*.xml"
  del "$release_dir\*.pdb"
  del "$release_dir\LinearLogFlow.dll.config"
}

Task Clean {
    if(Test-Path $release_dir) {
        Remove-Item $release_dir -Recurse -Force | Out-Null
    }
}

Task PrepareRelease {
    if(!(Test-Path $release_dir)) {
        New-Item $release_dir -ItemType Directory | Out-Null
    }
}




#---------------------------------

FormatTaskName {
   param($taskName)
   $s="$taskName "
   write-host ($s + ("-"* (70-$s.Length))) -foregroundcolor Cyan
}


Task ? -Description "Helper to display task info" {
  Write-Documentation
}


Task ShowMsBuildVersion -Description "Displays the version of msbuild" {
  msbuild /version
}

function Write-Info([string]$Message, $ForegroundColor="Magenta") {
   Write-Host $message  -ForegroundColor $ForegroundColor
}

function Normalize-Path([string]$Path){
  [System.IO.Path]::GetFullPath($Path)
}

function Get-DirPath([string]$Path){
  [System.IO.Path]::GetDirectoryName($Path)
}

function Get-FileName([string]$Path){
  [System.IO.Path]::GetFileName($Path)
}

function FindFile([string]$Path,[string]$FileName, [bool] $FailIfNotFound=$True){
  $file = (Get-ChildItem "$Path" -Filter "$FileName" -Recurse | Sort-Object -Property FullName -Descending | Select-Object -First 1).FullName
  if($FailIfNotFound -and ($file -eq $null)) {
    throw "Could not find a file matching $Path/**/$FileName"
  }
  return $file
}
