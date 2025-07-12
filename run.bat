@echo off
:: Navigate to the Backend folder and run WebBackUp.exe
cd service\src\WebBackUp\bin\Release\net8.0\publish
start "" "WebBackUp.exe"

:: Navigate back to the root directory and then to the Frontend folder to start the Angular app
cd ..\..\..\..\..\..\..\application\backup-app
::start "" "cmd /c npx serve -s dist/backup-app/browser/ -p 4200"
@echo off
mode con: cols=100 lines=30
start /min cmd /c "npx serve -s dist/backup-app/browser/ -p 4200"

timeout /t 3 /nobreak

:: Open localhost:4200 in Chrome
start chrome http://localhost:4200
