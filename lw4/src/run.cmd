@echo off
start /d Frontend dotnet Frontend.dll
start /d Backend dotnet Backend.dll
start /d TextListener dotnet TextListener.dll
start /d TextRankCalc dotnet TextRankCalc.dll
start http://127.0.0.1:5001