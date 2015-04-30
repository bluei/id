@echo off
 
del /Q /S /A:H *suo
 
 
 
del /Q /S Client\bin
 
rd /Q /S Client\bin
 
del /Q /S Client\obj
 
rd /Q /S Client\obj
 
 
del /Q /S Common\bin
 
rd /Q /S Common\bin
 
del /Q /S Common\obj
 
rd /Q /S Common\obj
 
 
 
del /Q /S Server\bin
 
rd /Q /S Server\bin
 
del /Q /S Server\obj
 
rd /Q /S Server\obj
 
 
 
del /Q /S bin\Debug
 
rd /Q /S bin\Debug
 
del /Q /S bin\Release
 
rd /Q /S bin\Release
 
 
del /Q /S _Pvt_Extensions
rd /Q /S _Pvt_Extensions
 
 
del /Q /S /A:H *.user
 
echo Cleanup completed.
 
pause