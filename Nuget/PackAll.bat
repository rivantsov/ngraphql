SET pver=0.9.1
Echo Version: "%pver%"
del /q Nupkg\*.*
:: Need to delete some MSBuild-generated temp files (with .cs extension)
del /q /s ..\TemporaryGeneratedFile_*.cs
nuget.exe pack NGraphQL.nuspec -Symbols -version %pver% -outputdirectory Nupkg
nuget.exe pack NGraphQL.Http.nuspec -Symbols -version %pver% -outputdirectory Nupkg

pause