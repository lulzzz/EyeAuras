@echo off
cd Sources
mklink /J /D PoeShared.Core "../Submodules/PoeEye/PoeEye/PoeShared.Core"
mklink /J /D PoeShared.Wpf "../Submodules/PoeEye/PoeEye/PoeShared.Wpf"
mklink /J /D PoeShared.Tests "../Submodules/PoeEye/PoeEye/PoeShared.Tests"
