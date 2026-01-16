#!/usr/bin/env bash
# Generates sudo-mcp.spec for COPR/Fedora
set -euo pipefail

usage() {
    echo "Usage: $0 <version> [--local-source <path>] [--sha256-x64 <hash>] [--sha256-arm64 <hash>]" >&2
    exit 1
}

if [ $# -lt 1 ]; then
    usage
fi

VERSION="$1"
LOCAL_SOURCE=""
SHA256_X64=""
SHA256_ARM64=""

shift
while [[ $# -gt 0 ]]; do
    case $1 in
        --local-source)
            LOCAL_SOURCE="$2"
            shift 2
            ;;
        --sha256-x64)
            SHA256_X64="$2"
            shift 2
            ;;
        --sha256-arm64)
            SHA256_ARM64="$2"
            shift 2
            ;;
        *)
            usage
            ;;
    esac
done

# Generate source lines based on local vs remote
if [[ -n "$LOCAL_SOURCE" ]]; then
    # For local sources, use just the filename (tarball must exist in ~/rpmbuild/SOURCES/)
    SOURCE_SECTION="# Architecture-specific sources (local)
%ifarch x86_64
Source0:        sudo-mcp-x64-v%{version}.tar.gz
%endif
%ifarch aarch64
Source0:        sudo-mcp-arm64-v%{version}.tar.gz
%endif"
else
    # For remote sources, use full URL
    SOURCE_SECTION="# Architecture-specific sources
%ifarch x86_64
Source0:        https://github.com/hughesjs/sudo-mcp/releases/download/v%{version}/sudo-mcp-x64-v%{version}.tar.gz
%endif
%ifarch aarch64
Source0:        https://github.com/hughesjs/sudo-mcp/releases/download/v%{version}/sudo-mcp-arm64-v%{version}.tar.gz
%endif"
fi

cat << EOF
Name:           sudo-mcp
Version:        ${VERSION}
Release:        1%{?dist}
Summary:        MCP server for privileged command execution via sudo/pkexec
License:        MIT
URL:            https://github.com/hughesjs/sudo-mcp

${SOURCE_SECTION}

BuildArch:      x86_64 aarch64
Requires:       polkit
Requires:       sudo
Requires:       libicu

# Don't strip .NET single-file binaries - they are self-contained and stripping breaks them
%global __os_install_post %{nil}
%define __strip /bin/true
%define debug_package %{nil}

%description
INHERENTLY UNSAFE: MCP server allowing AI models to execute privileged
commands via sudo/pkexec. Use at your own risk.

This tool integrates with Claude Desktop (or any MCP client) to enable
execution of privileged commands. It uses polkit/pkexec for authentication
and supports configurable command validation.

%prep
%ifarch x86_64
%setup -q -n sudo-mcp-x64-v%{version}
%endif
%ifarch aarch64
%setup -q -n sudo-mcp-arm64-v%{version}
%endif

%build
# Pre-built binary, no build step required

%install
install -Dm755 sudo-mcp %{buildroot}%{_bindir}/sudo-mcp
install -Dm644 README.md %{buildroot}%{_docdir}/%{name}/README.md
install -Dm644 SECURITY.md %{buildroot}%{_docdir}/%{name}/SECURITY.md
install -Dm644 LICENSE %{buildroot}%{_licensedir}/%{name}/LICENSE
install -dm755 %{buildroot}%{_localstatedir}/log/sudo-mcp

%files
%license LICENSE
%doc README.md SECURITY.md
%{_bindir}/sudo-mcp
%dir %{_localstatedir}/log/sudo-mcp

%changelog
* $(date "+%a %b %d %Y") James Hughes <james@pyrosoftsolutions.co.uk> - ${VERSION}-1
- Release version ${VERSION}
EOF
