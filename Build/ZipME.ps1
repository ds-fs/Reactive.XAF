[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Root=([System.IO.Path]::GetFullPath("$PSScriptRoot\..\")),
    [switch]$SkipIDEBuild
)


Write-HostFormatted "Building Xpand.XAF.ModelEditor.WinDesktop" -Section
Push-Location "$Root\Tools\Xpand.XAF.ModelEditor\"
if (!(Test-Path "$root\bin\zip")){
    New-Item "$root\bin\zip" -ItemType Directory -Verbose
}
for ($i = 6; $i -lt 9; $i++) {
    [xml]$publishXml = Get-Content "$pwd\Properties\PublishProfiles\FolderProfile.pubxml"
    $publishXml.Project.PropertyGroup.TargetFramework = "net$i.0-windows7.0"
    $publishXml.Project.PropertyGroup.PublishDir = "bin\Release\net$i.0-windows7.0\publish\" 
    $publishXml.Save("$pwd\Properties\PublishProfiles\FolderProfile.pubxml")

    [xml]$publishXml = Get-Content "$pwd\Xpand.XAF.ModelEditor.WinDesktop.csproj"
    $publishXml.Project.PropertyGroup|Where-Object{$_.TargetFramework}|ForEach-Object{
        $_.TargetFramework = "net$i.0-windows7.0"
        $_.OutputPath = "..\..\bin\net$i.0-windows7.0\"
    }
    $publishXml.Save("$pwd\Xpand.XAF.ModelEditor.WinDesktop.csproj")

    dotnet publish -p:PublishProfile="Folderprofile.pubxml" ".\Xpand.XAF.ModelEditor.WinDesktop.csproj" 
    Push-Location ".\bin\Release\net$i.0-windows7.0\publish"
    $zip=[System.IO.Path]::GetFullPath("$(Get-Location)\..\Xpand.XAF.ModelEditor.WinDesktop$i.zip")
    Compress-Files -zipfileName $zip -Force
    Copy-Item $zip "$root\bin\zip\Xpand.XAF.ModelEditor.WinDesktop$i.zip" -Verbose -Force
    Pop-Location
}

if (!(Test-AzDevops) -and !$SkipIDEBuild){
    Write-HostFormatted "Building Xpand.XAF.ModelEditor.Win" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\ModelEditor.Win\Xpand.XAF.ModelEditor.Win"
    dotnet publish -p:PublishProfile="Folderprofile.pubxml" ".\Xpand.XAF.ModelEditor.Win.csproj"
    Set-Location "$(Get-Location)\bin\Release\net6.0-windows\publish"
    Get-ChildItem|Copy-Item -Destination "$env:APPDATA\Xpand.XAF.ModelEditor.Win\Xpand.XAF.ModelEditor.Win" -Force -Recurse
    $zip="$(Get-Location)\..\Xpand.XAF.ModelEditor.Win.zip"
    Compress-Files -zipfileName $zip -Force 
    $version=[System.Diagnostics.FileVersionInfo]::GetVersionInfo("$(Get-Location)\Xpand.XAF.ModelEditor.Win.exe").FileVersion
    $dir="$([System.IO.Path]::GetDirectoryName($zip))\$([System.IO.Path]::GetFileNameWithoutExtension($zip)).$version.zip"
    $dir|Set-Clipboard
    Move-Item $zip $dir -Force
    $dest=[System.IO.Path]::GetFullPath("$([System.IO.Path]::GetDirectoryName($dir))\..\..\..\..\..\XVSIX64\Resources\$([System.IO.Path]::GetFileName($dir))")
    Copy-Item $dir $dest -Force 

    $proj=Get-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\XVSIX64\XVSIX64.csproj" -Raw
    $regex = [regex] 'Xpand\.XAF\.ModelEditor\.Win\..*\.zip'
    $allmatches = $regex.Matches($proj);
    $currentValue=$allmatches[0].Value
    $newValue="Xpand.XAF.ModelEditor.Win.$version.zip"
    $replacememnt=$proj.Replace($currentValue,$newValue)
    if ($replacememnt -notlike "*$newValue*"){
        throw "Resource replacement failed $newValue"
    }
    Set-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\XVSIX64\XVSIX64.csproj" $replacememnt
    

    $proj=Get-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.Rider.csproj" -Raw
    $allmatches = $regex.Matches($proj);
    $currentValue=$allmatches[0].Value
    $replacememnt=$proj.Replace($currentValue,$newValue)
    if ($replacememnt -notlike "*$newValue*"){
        throw "Resource replacement failed $newValue"
    }
    Set-Content  "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.Rider.csproj" $replacememnt
    
    $proj=Get-Content "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.csproj" -Raw
    $allmatches = $regex.Matches($proj);
    $currentValue=$allmatches[0].Value
    Set-Content       "$root\tools\Xpand.XAF.ModelEditor\IDE\Rider\src\dotnet\ReSharperPlugin.Xpand\ReSharperPlugin.Xpand.csproj" $proj.Replace($currentValue,$newValue)

    Write-HostFormatted "Building Rider" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\Rider"
    Start-Build
    Write-HostFormatted "Building XVSIX64" -Section
    Set-Location "$Root\tools\Xpand.XAF.ModelEditor\IDE\XVSIX64"
    Start-Build
}

Pop-Location