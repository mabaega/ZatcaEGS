@echo off
setlocal

REM Set the path to the executable and service name
set "serviceName=ZatcaEGS"
set "binPath=%~dp0ZatcaEGS.exe"

REM Check if the service already exists
sc query %serviceName% >nul 2>&1
if %errorlevel% equ 0 (
    echo The service "%serviceName%" already exists.
    
    REM Stop the service if it is running
    sc stop %serviceName% >nul 2>&1
    if %errorlevel% equ 0 (
        echo Stopping the service "%serviceName%".
        timeout /t 5 /nobreak >nul
    ) else (
        echo The service "%serviceName%" is not running.
    )
    
    REM Delete the existing service
    sc delete %serviceName% >nul 2>&1
    if %errorlevel% equ 0 (
        echo The service "%serviceName%" has been removed successfully.
    ) else (
        echo Failed to remove the service "%serviceName%".
        goto :end
    )
)

REM Create the service
sc create %serviceName% binPath= "%binPath%" start= auto
if %errorlevel% equ 0 (
    echo The service "%serviceName%" has been created successfully.
) else (
    echo Failed to create the service "%serviceName%".
    goto :end
)

REM Start the service
sc start %serviceName%
if %errorlevel% equ 0 (
    echo The service "%serviceName%" has been started successfully.
) else (
    echo Failed to start the service "%serviceName%".
)

:end
endlocal
pause