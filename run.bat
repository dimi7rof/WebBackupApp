@echo off
:: Navigate to the Backend folder and run WebBackUp.exe
cd service\src\WebBackUp\bin\Release\net8.0
start "" "WebBackUp.exe"

:: Navigate back to the root directory and then to the Frontend folder to start the Angular app
cd ..\..\..\..\..\..\application\backup-app
start "" "cmd /c ng serve"

timeout /t 10 /nobreak

:: Open localhost:4200 in Chrome
start chrome http://localhost:4200
