$fullpath = Resolve-Path "..\WpfApplication2\bin\x86\Release\"
$files = Get-ChildItem $fullpath -recurse | 
where {!$_.PSIsContainer}

if(Test-Path "filelist.lst"){ del "filelist.lst"}


foreach {$_.FullName.Substring($fullpath.Path.Length) >> "filelist.lst" }