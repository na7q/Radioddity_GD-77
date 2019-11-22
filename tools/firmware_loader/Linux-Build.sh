#!/usr/bin/env bash


msbuild GD77_FirmwareLoader.csproj -p:Configuration=Debug /p:Platform="AnyCPU"
exit $?
