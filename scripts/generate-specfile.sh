#!/usr/bin/env bash
# Generates sudo-mcp.spec for COPR/Fedora
set -euo pipefail

usage() {
    echo "Usage: $0 <version>" >&2
    exit 1
}

if [ $# -lt 1 ]; then
    usage
fi

VERSION="$1"

cat << EOF
Name:           sudo-mcp
Version:        ${VERSION}
Release:        1%{?dist}
Summary:        MCP server for privileged command execution via sudo/pkexec
License:        MIT
URL:            https://github.com/hughesjs/sudo-mcp

Source0:        https://github.com/hughesjs/sudo-mcp/releases/download/v%{version}/sudo-mcp-%{version}.tar.gz

ExclusiveArch:  x86_64 aarch64
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
%setup -q -n sudo-mcp-%{version}

%build
# Pre-built binary, no build step required

%install
%ifarch x86_64
install -Dm755 sudo-mcp-x64 %{buildroot}%{_bindir}/sudo-mcp
%endif
%ifarch aarch64
install -Dm755 sudo-mcp-arm64 %{buildroot}%{_bindir}/sudo-mcp
%endif
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
