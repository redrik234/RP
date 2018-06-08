@echo off
Setlocal EnableDelayedExpansion

start "Frontend" /d Frontend dotnet Frontend.dll
start "Backend" /d Backend dotnet Backend.dll

start "TextListener" /d TextListener dotnet TextListener.dll

start "TextRankCalc" /d TextRankCalc dotnet TextRankCalc.dll
set file=config/config_component.cfg
for /f "tokens=1,2 delims=:" %%a in (%file%) do (
for /l %%i in (1, 1, %%b) do start "%%a" /d %%a dotnet %%a.dll
)

start http://127.0.0.1:5001