@echo off
cd /d "%~dp0"

echo ==========================================
echo   MERGING GITHUB LICENSE FILE
echo ==========================================

:: 1. Pull the license file down to your PC
::    We use --allow-unrelated-histories because your PC and GitHub 
::    technically started as "strangers".
git pull origin main --allow-unrelated-histories --no-edit

:: 2. Now that you have the file, push your code up
git push origin main

echo.
echo ==========================================
echo   DONE! Your repo is now fully synced.
echo ==========================================
pause