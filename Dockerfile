FROM mcr.microsoft.com/windows/servercore:1909

# Copy a custom MSI transform for XNA 3.1 which disables a registration operation that fails on Windows Server Core
COPY XNA31_NoSelfReg.mst C:/XNA31_NoSelfReg.mst

# Install Chocolatey and build dependencies
RUN powershell iex(iwr -useb https://chocolatey.org/install.ps1)
RUN choco feature disable showDownloadProgress
RUN choco feature enable allowGlobalConfirmation
RUN powershell Set-Service wuauserv -StartupType Manual && DISM /Online /NoRestart /Enable-Feature /FeatureName:NetFx3ServerFeatures && choco install DotNet3.5 --version=3.5.20160716 && powershell Set-Service wuauserv -StartupType Disabled
RUN choco install git.portable --version=2.29.2.2
RUN choco install visualstudio-installer --version=2.0.1
RUN choco install visualstudio2019buildtools --version=16.8.1.0
RUN choco install visualstudio2019-workload-manageddesktopbuildtools --version=1.0.1
RUN choco install visualstudio2019-workload-vctools --version=1.0.0
RUN choco install netfx-4.7.2-devpack --version=4.7.2.20190225
RUN choco install 7zip.portable --version=19.0
RUN choco install lazarus --version=2.0.6
RUN choco install xna31 --version=3.1.0.20160105 --install-arguments="TRANSFORMS=C:\XNA31_NoSelfReg.mst"
RUN choco install xunit --version=2.3.1
ADD https://dist.nuget.org/win-x86-commandline/latest/nuget.exe C:/ProgramData/Chocolatey/bin/nuget.exe
ADD https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x86.exe C:/ProgramData/Chocolatey/bin/rcedit-x86.exe

# Set up paths using symbolid links because we can't dynamically set the path
RUN powershell "New-Item -ItemType SymbolicLink -Path 'C:\lazarus\strip' -Target (Get-ChildItem -Recurse 'C:\lazarus\fpc\strip.exe').DirectoryName"
RUN powershell "New-Item -ItemType SymbolicLink -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC\Current' -Target (Get-ChildItem -Directory 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC').FullName"
RUN SETX PATH "%PATH%;C:\lazarus;C:\lazarus\strip;C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\amd64;C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC\Current\bin\Hostx64\x64"
