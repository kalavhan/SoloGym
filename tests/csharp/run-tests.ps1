param([string]$SourceList, [string]$Entry = "Tests.Program")
$Sources = $SourceList.Split(",") | ForEach-Object { [System.IO.Path]::GetFullPath($_) }
$ErrorActionPreference = "Stop"
$pw = Split-Path ([System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName)
Add-Type -Path "$pw/Microsoft.CodeAnalysis.dll"; Add-Type -Path "$pw/Microsoft.CodeAnalysis.CSharp.dll"
$refs = New-Object System.Collections.Generic.List[Microsoft.CodeAnalysis.MetadataReference]
Get-ChildItem "$pw/ref" -Filter "*.dll" | ForEach-Object { $refs.Add([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile($_.FullName)) }
$opts = [Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::new([Microsoft.CodeAnalysis.CSharp.LanguageVersion]::CSharp9, [Microsoft.CodeAnalysis.DocumentationMode]::None, [Microsoft.CodeAnalysis.SourceCodeKind]::Regular, [string[]]@("SOLOGYM_TESTS"))
$trees = New-Object System.Collections.Generic.List[Microsoft.CodeAnalysis.SyntaxTree]
foreach ($f in $Sources) { $trees.Add([Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([System.IO.File]::ReadAllText($f), $opts, $f)) }
$copts = [Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$comp = [Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create("SoloGymTests" + [Guid]::NewGuid().ToString("N"), $trees, $refs, $copts)
$ms = New-Object System.IO.MemoryStream
$res = $comp.Emit($ms)
$errs = @($res.Diagnostics | Where-Object { $_.Severity -eq [Microsoft.CodeAnalysis.DiagnosticSeverity]::Error })
if ($errs.Count -gt 0) { $errs | ForEach-Object { Write-Output ("ERROR " + $_.ToString()) }; exit 1 }
$asm = [System.Reflection.Assembly]::Load($ms.ToArray())
$code = $asm.GetType($Entry).GetMethod("Main").Invoke($null, @())
exit $code
