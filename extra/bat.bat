@echo off
::设置要临时加入到path环境变量中的路径
set My_PATH=C:\Program Files\Autodesk\3ds Max 2014
set PATH=%PATH%;%My_PATH%
copy /y "%~dp0MaxServer.dll" "C:\Program Files\Autodesk\3ds Max 2014\bin\assemblies\"
copy /y "%~dp0startup.ms" "C:\Program Files\Autodesk\3ds Max 2014\"
::下面写你其它脚本命令
start 3dsmax
exit