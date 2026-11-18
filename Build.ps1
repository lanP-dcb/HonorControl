param([string]$OutputDirectory=(Join-Path $PSScriptRoot 'artifacts\HonorControl'),[switch]$WithoutCalibration)
$ErrorActionPreference='Stop'
$outputPath=[IO.Path]::GetFullPath($OutputDirectory)
$runningApps=@(Get-Process HonorControl -ErrorAction SilentlyContinue|Where-Object{try{[string]::Equals($_.Path,(Join-Path $outputPath 'HonorControl.exe'),[StringComparison]::OrdinalIgnoreCase)}catch{$false}})
if($runningApps.Count -gt 0){throw 'Close the application in the selected output directory before building, or choose a new output directory.'}
$backendPath=Join-Path $outputPath 'Backend'
$brokerStage=Join-Path $PSScriptRoot 'artifacts\broker'
New-Item -ItemType Directory -Path $backendPath -Force|Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources=@('HonorStandalone.cs','HonorFirmware.cs','PlatformProfile.cs','DisplayEffects.cs','DisplayGamma.cs')|ForEach-Object{Join-Path $PSScriptRoot ('BackendSource\'+$_)}
$sources+=Join-Path $PSScriptRoot 'Shared\DiagnosticLog.cs'
$sources+=Join-Path $PSScriptRoot 'Shared\PlatformCapabilities.cs'
& $compiler /nologo /platform:x64 /optimize+ /r:System.Core.dll /r:System.Management.dll /r:System.Web.Extensions.dll ('/out:'+(Join-Path $backendPath 'HonorStandalone.exe')) @sources
if($LASTEXITCODE -ne 0){throw 'Backend compilation failed.'}
& dotnet publish (Join-Path $PSScriptRoot 'Broker\HonorControl.Broker.csproj') -c Release -o $brokerStage -p:DebugType=None -p:DebugSymbols=false ('-p:PathMap='+$PSScriptRoot+'=/_/') --verbosity minimal
if($LASTEXITCODE -ne 0){throw 'Broker publish failed.'}
& dotnet publish (Join-Path $PSScriptRoot 'App\HonorControl.csproj') -c Release -o $outputPath -p:DebugType=None -p:DebugSymbols=false ('-p:PathMap='+$PSScriptRoot+'=/_/') --verbosity minimal
if($LASTEXITCODE -ne 0){throw 'WinUI publish failed.'}
Copy-Item -LiteralPath (Join-Path $brokerStage 'HonorControl.Broker.exe') -Destination $outputPath -Force
Get-ChildItem -LiteralPath $outputPath -File -Filter '*.pdb' | ForEach-Object {Remove-Item -LiteralPath $_.FullName -Force}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination $outputPath -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install.ps1') -Destination $outputPath -Force
foreach($file in @('README.en-US.md','INSTALL.zh-CN.md','INSTALL.en-US.md','CHANGELOG.md','LICENSE','THIRD-PARTY-NOTICES.md','Clean-HonorSoftware.ps1','Uninstall.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $outputPath -Force}
$unusedPatterns=@('onnxruntime*.dll','DirectML.dll','Microsoft.ML.OnnxRuntime.dll','Microsoft.Windows.AI*.dll','Microsoft.Windows.Widgets*.dll','Microsoft.WindowsAppRuntime.AI*.dll')
foreach($pattern in $unusedPatterns){Get-ChildItem -LiteralPath $outputPath -File -Filter $pattern -ErrorAction SilentlyContinue|ForEach-Object{Remove-Item -LiteralPath $_.FullName -Force}}
foreach($dir in @('docs','screenshots','NpuDetect')){ $p=Join-Path $outputPath $dir; if(Test-Path -LiteralPath $p){Remove-Item -LiteralPath $p -Recurse -Force} }
$keepLocales=@('en-us','zh-CN')
foreach($dir in Get-ChildItem -LiteralPath $outputPath -Directory){
    if($dir.Name -match '^[a-z]{2,3}(-[A-Za-z0-9]+)*$' -and $keepLocales -notcontains $dir.Name){Remove-Item -LiteralPath $dir.FullName -Recurse -Force}
}
foreach($pattern in @('Microsoft.Windows.AI*.winmd','Microsoft.Windows.Widgets*.winmd')){Get-ChildItem -LiteralPath $outputPath -File -Filter $pattern -ErrorAction SilentlyContinue|ForEach-Object{Remove-Item -LiteralPath $_.FullName -Force}}
$licenses=Join-Path $outputPath 'Licenses'
New-Item -ItemType Directory -Path $licenses -Force | Out-Null
$dotnetRoot=Split-Path (Get-Command dotnet).Source
foreach($file in @('LICENSE.txt','ThirdPartyNotices.txt')){Copy-Item -LiteralPath (Join-Path $dotnetRoot $file) -Destination (Join-Path $licenses ('dotnet-'+$file)) -Force}
$appSdk=Join-Path $env:USERPROFILE '.nuget\packages\microsoft.windowsappsdk\1.8.260921001'
foreach($file in @('license.txt','NOTICE.txt')){Copy-Item -LiteralPath (Join-Path $appSdk $file) -Destination (Join-Path $licenses ('WindowsAppSDK-'+$file)) -Force}
Write-Host ('Ready: '+(Join-Path $outputPath 'HonorControl.exe'))
