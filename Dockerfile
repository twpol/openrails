FROM mcr.microsoft.com/windows/servercore:1909

# Download the Chocolatey package version list here so that Docker can cache the installations
ADD https://chocolatey.org/api/v2/package-versions/7zip.portable/ package-versions/7zip.portable.json
ADD https://chocolatey.org/api/v2/package-versions/chocolatey/ package-versions/chocolatey.json
ADD https://chocolatey.org/api/v2/package-versions/git/ package-versions/git.json
ADD https://chocolatey.org/api/v2/package-versions/lazarus/ package-versions/lazarus.json
ADD https://chocolatey.org/api/v2/package-versions/nuget/ package-versions/nuget.json
ADD https://chocolatey.org/api/v2/package-versions/xna31/ package-versions/xna31.json

# Install Chocolatey itself
RUN powershell iex(iwr -useb https://chocolatey.org/install.ps1)

# Install build dependencies
RUN powershell Set-Service wuauserv -StartupType Manual
RUN DISM /Online /NoRestart /Enable-Feature /FeatureName:NetFx3ServerFeatures
RUN choco install --yes --no-progress 7zip.portable
RUN choco install --yes --no-progress git
RUN choco install --yes --no-progress lazarus
RUN choco install --yes --no-progress nuget
# RUN choco install --yes --no-progress xna31
RUN powershell Set-Service wuauserv -StartupType Disabled
RUN powershell (New-Object Net.WebClient).DownloadFile('https://github.com/electron/rcedit/releases/download/v1.1.1/rcedit-x86.exe', """$env:ChocolateyInstall/bin/rcedit-x86.exe""")

RUN DIR package-versions
RUN DIR %ChocolateyInstall%\bin
