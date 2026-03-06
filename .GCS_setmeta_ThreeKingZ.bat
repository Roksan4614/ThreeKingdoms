@echo off
chcp 65001 >nul

:: 록산_DEV set SET_PATH=D:\_Work\Kingz_ThreeKingdoms

set SET_PATH=D:\_Work\Kingz_ThreeKingdoms

set SET_URL_GS_DEV=dev-static-kingzgambit
set SET_URL_GS_STAGING=test-static-kingzgambit
set SET_URL_GS_LIVE=static-kingzgambit

set SET_URL_HTML_DEV=dev-static.kingz.games
set SET_URL_HTML_STAGING=test-static.kingz.games
set SET_URL_HTML_LIVE=static.kingz.games


set MSG_ERROR=NONE
set RESULT_PLATFORM=0

:GOTO_MENU

echo Ver.25.04.01_14:52
echo.
echo 프로젝트 경로: %SET_PATH%

if not "%MSG_ERROR%"=="NONE" (
	echo.
	echo %MSG_ERROR%
)

echo.
echo [1] Dev     [2] Staging     [3] Live     [4] 직접 입력
set /p SERVICE_TYPE=서비스 타입을 입력하세요: 

if "%SERVICE_TYPE%"=="1" (
	set GS_URL=%SET_URL_GS_DEV%
	set HTTPS_URL=%SET_URL_HTML_DEV%
	set SERVICE_TYPE=Dev
)
if "%SERVICE_TYPE%"=="2" (
	set GS_URL=%SET_URL_GS_STAGING%
	set HTTPS_URL=%SET_URL_HTML_STAGING%
	set SERVICE_TYPE=Live
)
if "%SERVICE_TYPE%"=="3" (
	set GS_URL=%SET_URL_GS_LIVE%
	set HTTPS_URL=%SET_URL_HTML_LIVE%
	set SERVICE_TYPE=Live
)

if not defined GS_URL set GS_URL=%SERVICE_TYPE%
echo GS_URL:%GS_URL%

echo.
echo [1] Android     [2] IOS     [3] WebGL
set /p PLATFORM=플랫폼을 선택해 주세요: 

if "%PLATFORM%"=="1" set RESULT_PLATFORM=Android
if "%PLATFORM%"=="2" set RESULT_PLATFORM=IOS
if "%PLATFORM%"=="3" set RESULT_PLATFORM=WebGL

if %RESULT_PLATFORM%==0 (
	cls
	set MSG_ERROR=플랫폼을 잘못 입력했어요.
	goto GOTO_MENU
)

set /p BUNDLE_VERION=어드레서블 버전을 입력하세요: 

if "%PLATFORM%"=="3" set /p VERSION=버전을 입력하세요: 

echo. 
echo. 

if "%PLATFORM%"=="3" (
	echo https://%HTTPS_URL%/WebGL/%VERSION%_%BUNDLE_VERION%/index.html

	echo.
	echo gsutil -m cp -r %SET_PATH%/0_Bin/WebGL/%SERVICE_TYPE% gs://%GS_URL%/WebGL/%VERSION%_%BUNDLE_VERION%
	echo gsutil -m setmeta -h "Content-Encoding:gzip" gs://%GS_URL%/WebGL/%VERSION%_%BUNDLE_VERION%/**/*.unityweb
	echo.
)

echo gsutil -m cp -r %SET_PATH%/Bundle/_Last/%RESULT_PLATFORM%  gs://%GS_URL%/Bundle/%RESULT_PLATFORM%/%BUNDLE_VERION%
echo gsutil -m setmeta -h "Content-Encoding:gzip" gs://%GS_URL%/Bundle/%RESULT_PLATFORM%/%BUNDLE_VERION%/*

:GOTO_FINISH

echo.
echo.
echo command 복사 후, 새 탭 열어서 붙혀넣기 하고 실행하기

echo.
set /p RESTART_TYPE=다시 하기

cls
set MSG_ERROR=NONE
goto GOTO_MENU