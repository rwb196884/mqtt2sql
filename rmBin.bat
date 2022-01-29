for /f  "delims=" %%d in ('dir *.vs /s /b /A:D') do rmdir /s /q "%%d"
for /f  "delims=" %%d in ('dir *bin /s /b /A:D') do rmdir /s /q "%%d"
for /f  "delims=" %%d in ('dir *obj /s /b /A:D') do rmdir /s /q "%%d"

PAUSE