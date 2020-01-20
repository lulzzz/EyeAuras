![](https://img.shields.io/github/release-date/iXab3r/EyeAuras.svg) ![](https://img.shields.io/github/downloads/iXab3r/EyeAuras/total.svg) ![](https://img.shields.io/github/last-commit/iXab3r/EyeAuras.svg)
[![Discord Chat](https://img.shields.io/discord/636487289689866240.svg)](https://discord.gg/pFHHebM)  

# More info and examples on [EyeAuras website](https://eyeauras.net/)

# Prerequisites
- Windows 7 or later
- Windows Aero (aka Desktop Composition) enabled - application uses DWM to create clones
- Microsoft .NET Core 3 Runtime + Desktop Runtime - [download](https://dotnet.microsoft.com/download/dotnet-core/3.0/runtime) *INSTALL BOTH BINARIES*

# Installation
- You can download the latest version of installer here - [download](https://github.com/iXab3r/EyeAuras/releases/latest).
- After initial installation application will periodically check Github for updates

### Examples of Productivity application
* Watch video in a small overlay while working
* Create on-top overlay containing some important information during presentation

### Example of Gaming application
* UI customization - you can clone and resize/move ANY part of ANY window => you can finally move some action bars to the center of the screen. Or bring HP bar closer to your eye line. Or make some important indicators (e.g. debuffs) much bigger and more visible.

## Features
- Clone any of your windows via global Hotkey or a set of Matching rules and keep it always-on-top while working with other applications
- Select and scale a subregion of the cloned window
- Click-through mode - make any of your overlays absolutely transparent to mouse clicks !
- Add Triggers(e.g. WindowIsActive) which can automatically show/hide overlay when a certain condition is met
- Add Actions(e.g. PlaySound) which will be executed when corresponding Trigger activates/deactivates
- Global Hotkeys support, you can bind custom keys to Hide/Show and Lock/Unlock ALL overlays across the system
- Global Hotkeys support for each individual aura - you can enable/disable each aura separately via a simple key press !
- Temporarily disable/enable auras to consume less resources
- Auto-Update - application periodically checks Github and allows you to effortlessly update to the latest version

![](https://i.imgur.com/qcpEynP.png)

### Aura is a combination of
* Overlay - real-time clone of ANY of your windows with almost zero-latency. For example, you can watch YouTube in a small overlay while working on something useful
* Trigger - turns On when certain condition is met, e.g. window with some specific title is active or some hotkey is pressed
* Action - some action which will be executed once Trigger is activated/deactivated, e.g. you can PlaySound when some window becomes active

#### Triggers

![Hotkey Is Active](https://i.imgur.com/bNKsww0.png)
![Window Is Active](https://i.imgur.com/g5628lB.png)

#### Actions

![Play Sound](https://i.imgur.com/jYnyzeM.png)
![Win Activate](https://i.imgur.com/vDts9Hi.png)

## How to build application
* I am extensively using [git-submodules](https://git-scm.com/docs/git-submodule "git-submodules") so you may have to run extra commands (git submodule update) if your git-client does not fully support this tech. I would highly recommend to use [Git Extensions](https://gitextensions.github.io/ "Git Extensions") which is awesome, free and open-source and makes submodules integration seamless
* The main "catch-up-moment" is that you need to run InitSymlinks.cmd before building an application - this is due to the fact that git symlinks are not supported on some older versions of Windows and I am using them to create links to submodules
* Application requires [.NET Core SDK 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0 ".NET Core SDK 3.0") 
* I am usually using [Jetbrains Rider](https://www.jetbrains.com/rider/ "Jetbrains Rider") so there MAY be some issues if you are using Microsoft Visual Studio, although I am trying to keep things compatible


## Licensing 
EyeAuras is licensed under the MS-RL (Microsoft Reciprocal License).

Cudos to authors of related software
* [LorenzCK](https://github.com/LorenzCK) for his awesome [OnTopReplica](https://github.com/LorenzCK/OnTopReplica)
* PowerAuras and WeakAuras addons in World Of Warcraft universe for the general idea 

## Problems
- Please see the [issues](https://github.com/iXab3r/EyeAuras/issues) page.
- If you've discovered something that's clearly wrong, or if you get an error, post a ticket.
- If you have a general comment or concern, feel free to leave a suggestion/enchancment
- Feel free to join our [Discord Community](https://discord.gg/pFHHebM) to talk, get help and discuss everything !
