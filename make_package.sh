MAIN=$PWD
dotnet build
cd bin/Debug/net9.0-windows10.0.19041.0
echo "start /b win-x64/ADOFAI-Macro.exe" > ADOFAI-Macro.bat
cd ..
rm $MAIN/"ADOFAI-Macro.NET9.zip"
$MAIN/zip.exe -c net9.0-windows10.0.19041.0 $MAIN/"ADOFAI-Macro.NET9.zip"