# Remove stale HSKMoreBalanceQuests junctions
$targets = @('C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\HSKMoreBalanceQuests')
$source = 'D:\Mods\HSKMoreBalanceQuests'
foreach ($t in $targets) {
    if (Test-Path $t) { cmd /c rmdir "$t" }
    cmd /c mklink /J "$t" "$source"
}
