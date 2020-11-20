FROM mcr.microsoft.com/windows/servercore:1909

# Build extras:
#   - A custom MSI transform for XNA 3.1 which disables a registration operation that fails on Windows Server Core
#   - Chocolatey package list
COPY / C:/Build

# Install Chocolatey and build dependencies
RUN powershell iex(iwr -useb https://chocolatey.org/install.ps1)
RUN powershell Set-Service wuauserv -StartupType Manual \
    && choco install --yes --no-progress --source=windowsfeatures NetFx3ServerFeatures \
    && choco install --yes --no-progress C:/Build/Chocolatey.config \
    && powershell Set-Service wuauserv -StartupType Disabled
ADD https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x86.exe C:/ProgramData/Chocolatey/bin/rcedit-x86.exe

# ADD https://dist.nuget.org/win-x86-commandline/latest/nuget.exe C:/ProgramData/Chocolatey/bin/nuget.exe

# Set up paths using symbolid links because we can't dynamically set the path
RUN powershell "New-Item -ItemType SymbolicLink -Path 'C:\lazarus\strip' -Target (Get-ChildItem -Recurse 'C:\lazarus\fpc\strip.exe').DirectoryName"
RUN powershell "New-Item -ItemType SymbolicLink -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC\Current' -Target (Get-ChildItem -Directory 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC').FullName"
RUN SETX PATH "%PATH%;C:\lazarus;C:\lazarus\strip;C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\amd64;C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC\Current\bin\Hostx64\x64"
