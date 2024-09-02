$projectFile = "Zatca.EGS.csproj"  # Replace with your actual .csproj filename
[xml]$xmlContent = Get-Content $projectFile

$currentDate = Get-Date
$year = $currentDate.ToString("yy")
$month = $currentDate.ToString("MM")
$day = $currentDate.ToString("dd")

$propertyGroup = $xmlContent.Project.PropertyGroup | Where-Object { $_.AssemblyVersion -and $_.FileVersion }

if ($propertyGroup) {
    $currentVersion = [Version]$propertyGroup.AssemblyVersion
    $currentDateVersion = "{0}.{1}.{2}" -f $year, $month, $day
    
    if ($currentVersion.Major.ToString("00") + $currentVersion.Minor.ToString("00") + $currentVersion.Build.ToString("00") -eq $currentDateVersion.Replace(".", "")) {
        # Same day, increment revision
        $newRevision = $currentVersion.Revision + 1
    } else {
        # New day, reset revision
        $newRevision = 1
    }
    
    $newVersion = "{0}.{1}.{2}.{3:D4}" -f $year, $month, $day, $newRevision
    
    $propertyGroup.AssemblyVersion = $newVersion
    $propertyGroup.FileVersion = $newVersion
    
    $xmlContent.Save($projectFile)
    Write-Output "Version updated to $newVersion"
} else {
    Write-Output "PropertyGroup with AssemblyVersion and FileVersion not found in $projectFile"
}
