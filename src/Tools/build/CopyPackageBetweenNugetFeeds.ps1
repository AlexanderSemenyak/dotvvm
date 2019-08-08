Param(
    [string]$version,
    [string]$server, 
    [string]$internalServer, 
    [string]$apiKey,
    [string]$packageId
)
    Write-Host "Copping package: $packageId.$version"   
    $webClient = New-Object System.Net.WebClient
        
    $url = "$internalServer/packages/$packageId/$version/$packageId.$version.nupkg"
    $nupkgFile = Join-Path $PSScriptRoot ($packageId + "." + $version + ".nupkg")

    Write-Host "Downloading from $url"
    $webClient.DownloadFile($url, $nupkgFile)
    Write-Host "Package downloaded from '$internalServer'."

    Write-Host "Uploading package..."
    & .\Tools\nuget.exe push $nupkgFile -source $server -apiKey $apiKey
    Write-Host "Package uploaded to $server."

    Remove-Item $nupkgFile