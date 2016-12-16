@echo off

REM build [elasticsearch_version]
REM build target [elasticsearch_version]


.paket\paket.bootstrapper.exe prerelease
if errorlevel 1 (
  exit /b %errorlevel%
)
.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

SET TARGET="BuildInstallers"
SET RELEASE=0
SET VERSION=
SET TESTTARGETS=

IF NOT [%1]==[] (
  echo %1 | findstr /r "^[0-9].*$" >nul
  if errorlevel 1 (  
    set TARGET="%1"
    if NOT [%2]==[] (set VERSION="%2")     
  ) else (
    set VERSION="%1"
  ) 
)

IF /I "%1"=="integrate" (
  IF NOT [%2]==[] (
    echo %2 | findstr /r "^[0-9].*$" >nul 
    if errorlevel 1 (
      set VERSION=
      set TESTTARGETS="%2"
    ) else (
      set VERSION="%2"

      if NOT [%3]==[] (set TESTTARGETS="%3")
    ) 
  )
)

IF /I "%1"=="release" (
  set RELEASE=1
  IF NOT [%2]==[] (
    echo %2 | findstr /r "^[0-9].*$" >nul 
    if errorlevel 1 (
      set VERSION=
    ) else (
      set VERSION="%2"
    ) 
  )
)


"packages\build\FAKE\tools\Fake.exe" "build\\scripts\\Targets.fsx" "target=%TARGET%" "version=%VERSION%" "testtargets=%TESTTARGETS%" "release=%RELEASE%"
