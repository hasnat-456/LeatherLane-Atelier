@echo off
echo Starting LeatherLane Atelier...
echo.

echo Starting Backend on port 5001...
start "LeatherLane Backend" cmd /k "cd /d "%~dp0backend" && npm start"

timeout /t 3 /nobreak >nul

echo Starting Frontend (Vite)...
start "LeatherLane Frontend" cmd /k "cd /d "%~dp0frontend" && npm run dev"

echo.
echo Both servers are starting!
echo Backend API: http://localhost:5001
echo Frontend App: Check frontend terminal for URL
echo.
pause
