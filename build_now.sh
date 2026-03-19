#!/bin/bash
cd "$(dirname "$0")"
/opt/homebrew/Cellar/dotnet@8/libexec  dotnet build --no-restore
