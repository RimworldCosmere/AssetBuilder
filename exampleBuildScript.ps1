$unityPath = $env:UNITY_PATH
$buildTarget = $Env:UNITY_BUILD_TARGET
if ( [string]::IsNullOrEmpty($unityPath))
{
    Write-Host "Set your UNITY_PATH environment variable"
    exit
}
if ( [string]::IsNullOrEmpty($buildTarget))
{
    Write-Host "Set your UNITY_BUILD_TARGET environment variable (windows, mac, linux)"
    exit
}

$unityArgs = @(
    "-batchmode",
    "-quit",
    '-projectPath="C:\AssetBuilder"',
    "-executeMethod=ModAssetBundleBuilder.BuildBundles",
    "-buildTarget=$buildTarget",
    "-source=C:\Users\author\mymod"
)

Write-Host "    Building asset bundle: $bundleName"
$process = Start-Process $unityPath -ArgumentList $unityArgs -Wait -PassThru

if ($process.ExitCode -ne 0)
{
    Write-Host "    Unity failed for $mod (exit code $( $process.ExitCode )). Crashing build."
    exit
}

Write-Host "    Finished generating assetbundle"