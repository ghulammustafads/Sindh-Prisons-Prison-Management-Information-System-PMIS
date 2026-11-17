#!/bin/bash
set -e

# Ensure .NET 8 SDK is installed in /usr/share/dotnet
if [ ! -f "/usr/share/dotnet/dotnet" ]; then
    echo "Initializing .NET 8 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir /usr/share/dotnet
fi

export DOTNET_ROOT="/usr/share/dotnet"
export PATH="/usr/share/dotnet:$PATH"
ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet 2>/dev/null || true

# If called as dev server runner, find newest compiled assembly or build
if [ "$1" = "run" ]; then
    DLL="bin/Release/net8.0/PrisonManagementSystem.dll"
    if [ ! -f "$DLL" ]; then
        DLL="bin/Debug/net8.0/PrisonManagementSystem.dll"
    elif [ -f "bin/Debug/net8.0/PrisonManagementSystem.dll" ] && [ "bin/Debug/net8.0/PrisonManagementSystem.dll" -nt "$DLL" ]; then
        DLL="bin/Debug/net8.0/PrisonManagementSystem.dll"
    fi

    if [ ! -f "$DLL" ]; then
        /usr/share/dotnet/dotnet build -c Release PrisonManagementSystem.csproj
        DLL="bin/Release/net8.0/PrisonManagementSystem.dll"
    fi

    exec /usr/share/dotnet/dotnet "$DLL" --urls=http://0.0.0.0:3000
fi

exec /usr/share/dotnet/dotnet "$@"
