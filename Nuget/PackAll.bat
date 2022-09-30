SET pver=1.5.0
Echo Version: "%pver%" 
del /q Nupkg\*.*
:: Need to delete some MSBuild-generated temp files (with .cs extension)
del /q /s ..\TemporaryGeneratedFile_*.cs
nuget.exe pack NGraphQL.nuspec -Symbols -version %pver% -outputdirectory Nupkg
nuget.exe pack NGraphQL.Client.nuspec -Symbols -version %pver% -outputdirectory Nupkg
nuget.exe pack NGraphQL.Server.nuspec -Symbols -version %pver% -outputdirectory Nupkg
nuget.exe pack NGraphQL.Server.AspNetCore.nuspec -Symbols -version %pver% -outputdirectory Nupkg

pause