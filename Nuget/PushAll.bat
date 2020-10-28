SET pver=0.9.3
Echo Version: "%pver%"
dir Nupkg\*.nupkg
@echo off
setlocal
:PROMPT
SET AREYOUSURE=N
SET /P AREYOUSURE=Are you sure (Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

echo Publishing....
:: When we push bin package, the symbols package is pushed automatically by the nuget util
nuget push Nupkg\NGraphQL.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
nuget push Nupkg\NGraphQL.Http.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
pause

:END
endlocal

