FROM mcr.microsoft.com/windows/servercore:1909

# Download the Chocolatey packages here so that Docker can cache the installations
ADD https://chocolatey.org/api/v2/package/7zip.portable/ packages/7zip.portable.nupkg
ADD https://chocolatey.org/api/v2/package/chocolatey-core.extension/ packages/chocolatey-core.extension.nupkg
ADD https://chocolatey.org/api/v2/package/chocolatey/ packages/chocolatey.nupkg
ADD https://chocolatey.org/api/v2/package/dotnet3.5/ packages/dotnet3.5.nupkg
ADD https://chocolatey.org/api/v2/package/git.install/ packages/git.install.nupkg
ADD https://chocolatey.org/api/v2/package/lazarus/ packages/lazarus.nupkg
ADD https://chocolatey.org/api/v2/package/xna31/ packages/xna31.nupkg

# Install Chocolatey itself
ENV chocolateyDownloadUrl file:///C:/packages/chocolatey.nupkg
RUN powershell iex(iwr -useb https://chocolatey.org/install.ps1)

# Add source for our locally downloaded packages
RUN choco source add --name=local --source=C:\packages

# Install build dependencies
RUN powershell Set-Service wuauserv -StartupType Manual
RUN DISM /Online /NoRestart /Enable-Feature /FeatureName:NetFx3ServerFeatures
RUN choco install --source=local --yes --no-progress git.install lazarus 7zip.portable xna31
RUN powershell Set-Service wuauserv -StartupType Disabled
RUN powershell (New-Object Net.WebClient).DownloadFile('https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x86.exe', 'tools/rcedit-x86.exe')

RUN DIR C:\packages
RUN DIR C:\tools
