set game_dir=%1
set game_dir
set game_dir=%game_dir:"=%
set game_dir

SET mypath=%~dp0

xcopy "%game_dir%\MelonLoader\net6\" "%mypath%\ref\net6" /s /e /y
xcopy "%game_dir%\MelonLoader\Il2CppAssemblies\" "%mypath%\ref\Il2CppAssemblies" /s /e /y