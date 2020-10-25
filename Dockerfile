FROM mcr.microsoft.com/windows/servercore:1909

# Download the Chocolatey package version list here so that Docker can cache the installations
ADD https://chocolatey.org/api/v2/package-versions/chocolatey/ package-versions/chocolatey.json
ADD https://chocolatey.org/api/v2/package-versions/7zip.portable/ package-versions/7zip.portable.json
ADD https://chocolatey.org/api/v2/package-versions/git/ package-versions/git.json
ADD https://chocolatey.org/api/v2/package-versions/lazarus/ package-versions/lazarus.json
ADD https://chocolatey.org/api/v2/package-versions/xna31/ package-versions/xna31.json
ADD https://chocolatey.org/api/v2/package-versions/xunit/ package-versions/xunit.json
ADD https://chocolatey.org/api/v2/package-versions/visualstudio2019buildtools/ package-versions/visualstudio2019buildtools.json

# Install Chocolatey itself
RUN powershell iex(iwr -useb https://chocolatey.org/install.ps1)

# Install build dependencies
RUN powershell Set-Service wuauserv -StartupType Manual
RUN DISM /Online /NoRestart /Enable-Feature /FeatureName:NetFx3ServerFeatures
RUN choco install --yes --no-progress 7zip.portable
RUN choco install --yes --no-progress git
RUN choco install --yes --no-progress lazarus
# RUN choco install --yes --no-progress xna31
RUN choco install --yes --no-progress xunit
RUN choco install --yes --no-progress visualstudio-installer
RUN choco install --yes --no-progress visualstudio2019buildtools
RUN choco install --yes --no-progress visualstudio2019-workload-manageddesktopbuildtools
RUN choco install --yes --no-progress visualstudio2019-workload-vctools
RUN powershell Set-Service wuauserv -StartupType Disabled
ADD https://dist.nuget.org/win-x86-commandline/latest/nuget.exe $ChocolateyInstall/bin/nuget.exe
ADD https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x86.exe $ChocolateyInstall/bin/rcedit-x86.exe

# Set up links (because we can't dynamically set the path) for the global paths
RUN powershell New-Item -ItemType SymbolicLink -Path "C:\lazarus\fpc\strip" -Target (Get-ChildItem -Recurse "C:\lazarus\fpc\strip.exe").DirectoryName
RUN SETX PATH "%PATH%;C:\lazarus;C:\lazarus\fpc\strip"
