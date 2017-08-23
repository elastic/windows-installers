@echo off

.paket\paket.bootstrapper.exe
IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%

IF EXIST paket.lock (
	.paket\paket.exe restore
	IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%
)
IF NOT EXIST paket.lock (
	.paket\paket.exe install
	IF %ERRORLEVEL% NEQ 0 EXIT /B %ERRORLEVEL%
)
"packages\build\FAKE\tools\Fake.exe" "build\\scripts\\Targets.fsx" "cmdline=%*"
