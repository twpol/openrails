FROM mcr.microsoft.com/windows/servercore:1909

# Install Chocolatey
ADD https://chocolatey.org/api/v2/package-versions/chocolatey/ package-versions/chocolatey.json
RUN powershell iex(iwr -useb https://chocolatey.org/install.ps1)

# Install build dependency XNA 3.1
ADD https://chocolatey.org/api/v2/package-versions/xna31/ package-versions/xna31.json
COPY XNA31_NoSelfReg.mst C:/XNA31_NoSelfReg.mst
RUN powershell Set-Service wuauserv -StartupType Manual \
    && DISM /Online /NoRestart /Enable-Feature /FeatureName:NetFx3ServerFeatures \
    && choco install --yes --no-progress --install-arguments="TRANSFORMS=C:\XNA31_NoSelfReg.mst" xna31 \
    && powershell Set-Service wuauserv -StartupType Disabled

# Install build dependency Lazarus
ADD https://chocolatey.org/api/v2/package-versions/lazarus/ package-versions/lazarus.json
RUN choco install --yes --no-progress lazarus

# Install build dependency 7zip
ADD https://chocolatey.org/api/v2/package-versions/7zip.portable/ package-versions/7zip.portable.json
RUN choco install --yes --no-progress 7zip.portable

# Install build dependency Git
ADD https://chocolatey.org/api/v2/package-versions/git/ package-versions/git.json
RUN choco install --yes --no-progress git

# Install build dependency XUnit
ADD https://chocolatey.org/api/v2/package-versions/xunit/ package-versions/xunit.json
RUN choco install --yes --no-progress xunit

# Install build dependency Visual Studio Managed Desktop Build Tools and VC Tools
ADD https://chocolatey.org/api/v2/package-versions/visualstudio2019buildtools/ package-versions/visualstudio2019buildtools.json
RUN choco install --yes --no-progress visualstudio-installer visualstudio2019buildtools visualstudio2019-workload-manageddesktopbuildtools visualstudio2019-workload-vctools netfx-4.7.2-devpack

# Install build dependency NuGet
ADD https://dist.nuget.org/win-x86-commandline/latest/nuget.exe C:/ProgramData/Chocolatey/bin/nuget.exe

# Install build dependency RcEdit
ADD https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x86.exe C:/ProgramData/Chocolatey/bin/rcedit-x86.exe

# Set up links (because we can't dynamically set the path) for the global paths
RUN powershell "New-Item -ItemType SymbolicLink -Path 'C:\lazarus\strip' -Target (Get-ChildItem -Recurse 'C:\lazarus\fpc\strip.exe').DirectoryName"
RUN powershell "New-Item -ItemType SymbolicLink -Path 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC\Current' -Target (Get-ChildItem -Directory 'C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC').FullName"
RUN SETX PATH "%PATH%;C:\lazarus;C:\lazarus\strip;C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\amd64;C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\VC\Tools\MSVC\Current\bin\Hostx64\x64"
