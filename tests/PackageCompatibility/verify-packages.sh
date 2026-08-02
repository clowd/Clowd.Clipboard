#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
package_dir="$repo_root/build/Release"
dotnet="${DOTNET:-dotnet}"

sdk9="$($dotnet --list-sdks | awk '$1 ~ /^9[.]0[.]/ { print $1 }' | sort -V | tail -1)"
if [[ -z "$sdk9" ]]; then
  echo "The .NET 9 SDK is required for Avalonia 12 package compatibility tests." >&2
  exit 1
fi

core_package="$(find "$package_dir" -maxdepth 1 -type f -name 'Clowd.Clipboard.[0-9]*.nupkg' | sort -V | tail -1)"
if [[ -z "$core_package" ]]; then
  echo "No packed Clowd.Clipboard package found in $package_dir. Build Release first." >&2
  exit 1
fi

version="$(basename "$core_package")"
version="${version#Clowd.Clipboard.}"
version="${version%.nupkg}"

temp_root="$(mktemp -d)"
trap 'rm -rf "$temp_root"' EXIT
export NUGET_PACKAGES="$temp_root/packages"

printf '{\n  "sdk": {\n    "version": "%s",\n    "rollForward": "disable"\n  }\n}\n' "$sdk9" > "$temp_root/global.json"
cd "$temp_root"

for name in Clowd.Clipboard Clowd.Clipboard.Gdi Clowd.Clipboard.Wpf Clowd.Clipboard.Avalonia; do
  package="$package_dir/$name.$version.nupkg"
  if [[ ! -f "$package" ]]; then
    echo "Missing packed package: $package" >&2
    exit 1
  fi

  nuspec="$temp_root/$name.nuspec"
  unzip -p "$package" '*.nuspec' > "$nuspec"
  if grep -Eq 'targetFramework="(net6[.]0|[.]NETFramework4[.]6[.]1)' "$nuspec"; then
    echo "$name still advertises an unsupported net6.0 or .NET Framework 4.6.1 target." >&2
    exit 1
  fi
done

if ! grep -Eq '<dependency id="Avalonia" version="12[.]' "$temp_root/Clowd.Clipboard.Avalonia.nuspec"; then
  echo "Clowd.Clipboard.Avalonia must depend on Avalonia 12." >&2
  exit 1
fi

verify_package() {
  local name="$1"
  local framework="$2"
  local directory="$temp_root/$name-consumer"
  mkdir -p "$directory"

  printf '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>%s</TargetFramework>\n    <EnableWindowsTargeting>true</EnableWindowsTargeting>\n  </PropertyGroup>\n  <ItemGroup>\n    <PackageReference Include="%s" Version="%s" />\n  </ItemGroup>\n</Project>\n' \
    "$framework" "$name" "$version" > "$directory/Consumer.csproj"

  "$dotnet" new nugetconfig --output "$directory" --force >/dev/null
  "$dotnet" nuget add source "$package_dir" \
    --name local-clowd \
    --configfile "$directory/nuget.config" >/dev/null
  "$dotnet" restore "$directory/Consumer.csproj" \
    --force \
    --no-cache \
    --verbosity minimal
  "$dotnet" build "$directory/Consumer.csproj" \
    --no-restore \
    --warnaserror \
    --verbosity minimal
}

verify_package Clowd.Clipboard.Avalonia net8.0
verify_package Clowd.Clipboard.Wpf net8.0-windows
