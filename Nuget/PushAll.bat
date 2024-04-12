SET pver=1.7.0
Echo Version: "%pver%"
dir packages\*.nupkg
@echo off
setlocal
:PROMPT
SET AREYOUSURE=N
SET /P AREYOUSURE=Are you sure (Y/[N])?
IF /I "%AREYOUSURE%" NEQ "Y" GOTO END

cd packages

echo Publishing....
:: When we push bin package, the symbols package is pushed automatically by the nuget util
nuget push NGraphQL.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
nuget push NGraphQL.Client.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
nuget push NGraphQL.Server.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
nuget push NGraphQL.Server.AspNetCore.%pver%.nupkg -source https://api.nuget.org/v3/index.json 
pause

:END
endlocal

