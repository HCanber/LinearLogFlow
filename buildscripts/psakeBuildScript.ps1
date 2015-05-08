Properties {
  $build_dir = Split-Path $psake.build_script_file

  $root_dir = Normalize-Path "$build_dir\.."
  $src_dir =  "$root_dir\src"

  $sln_path =  FindFile "$src_dir" *.sln
  $configuration = "Release"
  $nuspec = (Get-Childitem -Path "$src_dir" -Filter *.nuspec -Recurse | Select-Object -First 1)

  $packages_dir = "$src_dir\packages"
  $release_dir = "$root_dir\_release"
  $release_nuget_dir = "$root_dir\_release_nuget"
  $nuget_exe=$global:build_defaults["nuget_exe"]
}


Task default -Depends Release

Task Release -Depends Clean, PrepareRelease, Compile, RemoveUnnecessaryFiles, CreateNugetPackage {
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
  if(Test-Path $release_nuget_dir) {
      Remove-Item $release_nuget_dir -Recurse -Force | Out-Null
  }
  #Exec { msbuild $sln_path /p:Configuration=$configuration /v:quiet /target:Clean }
}

Task PrepareRelease {
    if(!(Test-Path $release_dir)) {
        New-Item $release_dir -ItemType Directory | Out-Null
    }
    if(!(Test-Path $release_nuget_dir)) {
        New-Item $release_nuget_dir -ItemType Directory | Out-Null
    }
}

Task CreateNugetPackage -Depends Compile {
  $releaseNuspec = $release_dir +"\" + $nuspec.Name
  copy $nuspec.FullName $release_dir
  $assemblyInfoPath=$nuspec.DirectoryName + "\Properties\AssemblyInfo.cs"
  Write-info $assemblyInfoPath
  $version = Get-VersionNumberFromAssemblyInfo $assemblyInfoPath
  Write-Info ("Using version number: "+ $version.AssemblyInformationalVersion)
  Update-Nuspec-VersionNumber $releaseNuspec $version.AssemblyInformationalVersion
  $nuget=$global:build_defaults["nuget_exe"]
  Exec { &$nuget pack $releaseNuspec -OutputDirectory $release_nuget_dir}
  rm $release_dir/*.nuspec
  Write-Info "Created nuget packages in $release_nuget_dir"
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

function Update-Nuspec-VersionNumber($nuget_spec,$versionNumber)  {
  # update nuget spec version number
  [xml] $spec = Get-Content $nuget_spec
  $spec.package.metadata.version = $versionNumber
  Set-ItemProperty $nuget_spec IsReadOnly $false
  $spec.Save($nuget_spec)  
}

function Get-VersionNumberFromAssemblyInfo {
  [CmdletBinding()]
  Param(
    [Parameter(Mandatory=$True)]
    [string]$AssemblyInfoPath,
    
    [Parameter()]
    [switch]
    $useAssemblyVersionIfNoInfoVersion=$true
  )

  $assemblyVersionPattern = 'AssemblyVersion\s*\("([^"]+)"\)'  
  $assemblyInfoVersionPattern = 'AssemblyInformationalVersion\s*\("([^"]+)"\)'
  $file=Get-Content $assemblyInfoPath

  $versionNumbers= @{
    AssemblyVersion =  $file | select-string $assemblyVersionPattern | select -first 1 | % { $_.Matches[0].Groups[1].Value }
    AssemblyInformationalVersion = $file | select-string $assemblyInfoVersionPattern | select -first 1 | % { $_.Matches[0].Groups[1].Value }
  }
  if($useAssemblyVersionIfNoInfoVersion -and ($versionNumbers.AssemblyInformationalVersion -eq $null)) { $versionNumbers.AssemblyInformationalVersion = $versionNumbers.AssemblyVersion }

  return New-Object PSObject -Property $versionNumbers
}
