@echo off
if %~1 == "" goto err

start /wait /d Frontend dotnet publish -o ..\..\%~1\Frontend
start /wait /d Backend dotnet publish -o ..\..\%~1\Backend

start /wait xcopy config ..\%~1\config /I
start /wait xcopy run.cmd ..\%~1
start /wait xcopy stop.cmd ..\%~1

echo "compilation and build complete"
pause
exit 0

:err
echo "Argument not found"
exit 1