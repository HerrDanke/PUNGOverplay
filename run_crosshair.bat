@echo off
chcp 65001 >nul
echo ================================
echo  PUBG Crosshair Overlay
echo ================================
echo.
echo 启动中...
start "" "%~dp0Crosshair.exe"
echo.
echo 已启动！
echo.
echo 使用方法:
echo   ` (反引号) 或 F2  - 显示/隐藏红点
echo   ESC           - 退出程序
echo.
echo 启动后默认隐藏，按 ` 键显示红点
echo.
pause
