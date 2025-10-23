$dt = Get-Date -Format 'yyyyMMdd-HHmm'
$backup = "designer-backup-$dt"
$red = "comic-redesign-$dt"
Write-Output "Creating backup branch: $backup"
git checkout -b $backup
Write-Output "Staging all changes..."
git add -A
if ((git status --porcelain) -ne '') {
    git commit -m "chore: backup UI/UX designer changes ($dt)"
} else {
    Write-Output "No changes to commit"
}
Write-Output "Creating redesign branch: $red"
git checkout -b $red
Write-Output "Branches created: $backup, $red"