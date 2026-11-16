@echo off
chcp 65001 >nul
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
python -m PyInstaller --onefile --windowed --name RussiaOnlineAntiAFK anti_afk.py
echo.
echo Готово! EXE находится в папке dist.
pause
