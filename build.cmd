@if "%SCM_TRACE_LEVEL%" NEQ "4" @echo off
setlocal enabledelayedexpansion

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: Deployment
:: ----------

:: Restore NuGet packages
.paket\paket.bootstrapper.exe
.paket\paket.exe restore

:: Copy static site content over
xcopy src\webhost "%DEPLOYMENT_TEMP%\" /Y /E /Q /EXCLUDE:excludes.txt

:: Deploy an F# script as a continuously running Web Job
xcopy src\Sample.fsx "%DEPLOYMENT_TEMP%\app_data\jobs\continuous\Sample\" /Y

:: Build to the temporary path
cd "%DEPLOYMENT_SOURCE%"
call :ExecuteCmd "%MSBUILD_PATH%" /m /t:Build /p:Configuration=Release;OutputPath="%DEPLOYMENT_TEMP%";UseSharedCompilation=false %SCM_BUILD_ARGS% /v:m
IF !ERRORLEVEL! NEQ 0 goto error
cd ..

:: KuduSync
call :ExecuteCmd "%KUDU_SYNC_CMD%" -v 50 -f "%DEPLOYMENT_TEMP%" -t "%DEPLOYMENT_TARGET%" -n "%NEXT_MANIFEST_PATH%" -p "%PREVIOUS_MANIFEST_PATH%" -i ".git;.hg;.deployment;deploy.cmd"
IF !ERRORLEVEL! NEQ 0 goto error

::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::


:: Post deployment stub
IF DEFINED POST_DEPLOYMENT_ACTION call "%POST_DEPLOYMENT_ACTION%"
IF !ERRORLEVEL! NEQ 0 goto error

goto end

:: Execute command routine that will echo out when error
:ExecuteCmd
setlocal
set _CMD_=%*
call %_CMD_%
if "%ERRORLEVEL%" NEQ "0" echo Failed exitCode=%ERRORLEVEL%, command=%_CMD_%
exit /b %ERRORLEVEL%

:error
endlocal
echo An error has occurred during web site deployment.
call :exitSetErrorLevel
call :exitFromFunction 2>nul

:exitSetErrorLevel
exit /b 1

:exitFromFunction
()

:end
endlocal
echo Finished successfully.
