@echo off
cd Sources
mklink /J /D PoeShared.Core "../Submodules/PoeEye/PoeEye/PoeShared.Core"
mklink /J /D PoeShared.Wpf "../Submodules/PoeEye/PoeEye/PoeShared.Wpf"
mklink /J /D PoeShared.Tests "../Submodules/PoeEye/PoeEye/PoeShared.Tests"

echo Initializing Optional modules
mklink /J /D PoeShared.OpenCV "../Submodules/EyeAuras.Plus/PoeShared.OpenCV"
mklink /J /D EyeAuras.OpenCVAuras "../Submodules/EyeAuras.Plus/EyeAuras.OpenCVAuras"
mklink /J /D EyeAuras.Loader "../Submodules/EyeAuras.Plus/EyeAuras.Loader"
mklink /J /D EyeAuras.Web "../Submodules/EyeAuras.Plus/EyeAuras.Web"
