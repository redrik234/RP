@echo off
if "%~1" == "" goto err

echo "Compilation ..."

start /wait /d Frontend dotnet publish -o ..\..\%~1\Frontend
start /wait /d Backend dotnet publish -o ..\..\%~1\Backend
start /wait /d TextListener dotnet publish -o ..\..\%~1\TextListener
start /wait /d TextRankCalc dotnet publish -o ..\..\%~1\TextRankCalc
start /wait /d VowelConsCounter dotnet publish -o ..\..\%~1\VowelConsCounter
start /wait /d VowelConsRater dotnet publish -o ..\..\%~1\VowelConsRater
start /wait /d TextStatistics dotnet publish -o ..\..\%~1\TextStatistics
start /wait /d TextProcessingLimiter dotnet publish -o ..\..\%~1\TextProcessingLimiter
start /wait /d TextSuccessMarker dotnet publish -o ..\..\%~1\TextSuccessMarker

echo "Copy files ..."
@echo off
start /wait xcopy config ..\%~1\config /I
start /wait xcopy run.cmd ..\%~1
start /wait xcopy stop.cmd ..\%~1

echo "Compilation and build complete"
pause
exit 0

:err
echo "Not fount argument"
pause
exit 1