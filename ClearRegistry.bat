@echo off
reg delete HKCU\Software\Stardock\Settings\TrialEmail /f

echo Detecting Stardock Installations...

set "reset_trial="
for %%A in ("C:\ProgramData\Stardock\WindowBlinds|WB11Config.exe|WindowBlinds 11", "C:\ProgramData\Stardock\Start11|Start11Config.exe|Start11") do (
    for /f "tokens=1-3 delims=|" %%B in ("%%A") do (
        if exist "%%B" (
            echo   %%D detected
            tasklist | findstr "%%C" > nul
            if errorlevel 1 (
                rmdir /s /q "%%B"
                echo   %%D trial license has been reset
                set "reset_trial=1"
            ) else (
                echo Please exit %%D before running this script
                exit 52
            )
        )
    )
)

if not defined reset_trial (
    echo No Stardock installations detected or trial reset was unsuccessful.
    exit 51
)
pause
exit 0