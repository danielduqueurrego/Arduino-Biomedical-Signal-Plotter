#!/usr/bin/env bash
set -euo pipefail

VERSION="${VERSION:-v0.1.0}"
CONFIGURATION="${CONFIGURATION:-Release}"
APP_NAME="Biomedical Instrumentation Signal Plotter"
EXECUTABLE_NAME="BiomedicalSignalPlotter"
BUNDLE_IDENTIFIER="edu.bmeg.biomedical-instrumentation-signal-plotter"

usage() {
    cat <<'EOF'
Usage:
  scripts/package-macos.sh [osx-arm64|osx-x64|all]

Defaults to osx-arm64. Set VERSION or CONFIGURATION in the environment to
override the default v0.1.0 Release package.
EOF
}

runtime_arg="${1:-osx-arm64}"
case "$runtime_arg" in
    osx-arm64)
        runtimes=("osx-arm64")
        ;;
    osx-x64)
        runtimes=("osx-x64")
        ;;
    all)
        runtimes=("osx-arm64" "osx-x64")
        ;;
    -h|--help)
        usage
        exit 0
        ;;
    *)
        usage
        echo "Unsupported runtime: $runtime_arg" >&2
        exit 1
        ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
app_project="$repo_root/src/BiomedicalSignalPlotter/BiomedicalSignalPlotter.csproj"
artifacts_root="$repo_root/artifacts"
version_number="${VERSION#v}"

run_checked() {
    echo
    printf '>'
    printf ' %q' "$@"
    echo
    "$@"
}

copy_dir() {
    local source="$1"
    local destination="$2"

    if [[ -d "$source" ]]; then
        rm -rf "$destination"
        mkdir -p "$(dirname "$destination")"
        cp -R "$source" "$destination"
    fi
}

copy_file_if_exists() {
    local source="$1"
    local destination="$2"

    if [[ -f "$source" ]]; then
        mkdir -p "$(dirname "$destination")"
        cp "$source" "$destination"
    fi
}

create_zip() {
    local release_root="$1"
    local zip_path="$2"
    local package_name
    package_name="$(basename "$release_root")"

    rm -f "$zip_path"

    if command -v ditto >/dev/null 2>&1; then
        (cd "$artifacts_root" && ditto -c -k --sequesterRsrc --keepParent "$package_name" "$zip_path")
    elif command -v zip >/dev/null 2>&1; then
        (cd "$artifacts_root" && zip -qry "$zip_path" "$package_name")
    else
        echo "Neither ditto nor zip is available; cannot create $zip_path" >&2
        exit 1
    fi
}

if [[ ! -f "$app_project" ]]; then
    echo "App project not found: $app_project" >&2
    exit 1
fi

if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "Warning: this script is intended for macOS. Final .app launch validation should be run on macOS." >&2
fi

mkdir -p "$artifacts_root"

pushd "$repo_root" >/dev/null
run_checked dotnet restore
run_checked dotnet build --configuration "$CONFIGURATION" --no-restore
run_checked dotnet test --configuration "$CONFIGURATION" --no-build
popd >/dev/null

for runtime in "${runtimes[@]}"; do
    arch="${runtime#osx-}"
    package_name="Biomedical-Instrumentation-Signal-Plotter-$VERSION-macos-$arch"
    release_root="$artifacts_root/$package_name"
    publish_dir="$release_root/publish"
    bundle_path="$release_root/$APP_NAME.app"
    contents_dir="$bundle_path/Contents"
    macos_dir="$contents_dir/MacOS"
    resources_dir="$contents_dir/Resources"
    zip_path="$artifacts_root/$package_name.zip"

    rm -rf "$release_root"
    rm -f "$zip_path"
    mkdir -p "$release_root" "$publish_dir" "$macos_dir" "$resources_dir"

    run_checked dotnet publish "$app_project" \
        --configuration "$CONFIGURATION" \
        --runtime "$runtime" \
        --self-contained true \
        --output "$publish_dir" \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true

    cp -R "$publish_dir"/. "$macos_dir"/
    rm -rf "$publish_dir"

    if [[ ! -f "$macos_dir/$EXECUTABLE_NAME" ]]; then
        echo "Expected published executable not found: $macos_dir/$EXECUTABLE_NAME" >&2
        exit 1
    fi

    chmod +x "$macos_dir/$EXECUTABLE_NAME"

    copy_dir "$repo_root/src/BiomedicalSignalPlotter/Assets" "$macos_dir/Assets"

    icon_plist=""
    if [[ -f "$repo_root/src/BiomedicalSignalPlotter/Assets/app-icon.icns" ]]; then
        cp "$repo_root/src/BiomedicalSignalPlotter/Assets/app-icon.icns" "$resources_dir/app-icon.icns"
        icon_plist="    <key>CFBundleIconFile</key>
    <string>app-icon</string>"
    fi

    cat > "$contents_dir/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleExecutable</key>
    <string>$EXECUTABLE_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$BUNDLE_IDENTIFIER</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$version_number</string>
    <key>CFBundleVersion</key>
    <string>$version_number</string>
$icon_plist
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

    copy_dir "$repo_root/firmware" "$release_root/firmware"
    copy_dir "$repo_root/docs" "$release_root/docs"
    copy_file_if_exists "$repo_root/README.md" "$release_root/README.md"
    copy_file_if_exists "$repo_root/scripts/upload-uno-r4-wifi.ps1" "$release_root/scripts/upload-uno-r4-wifi.ps1"

    for license_name in LICENSE LICENSE.md LICENSE.txt; do
        copy_file_if_exists "$repo_root/$license_name" "$release_root/$license_name"
    done

    create_zip "$release_root" "$zip_path"

    echo
    echo "Release folder: $release_root"
    echo "Release ZIP:    $zip_path"
done

echo
echo "macOS package workflow complete."
