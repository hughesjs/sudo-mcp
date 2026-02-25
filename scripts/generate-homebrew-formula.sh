#!/usr/bin/env bash
set -euo pipefail

usage() {
    echo "Usage: $0 <version> --sha256-x64 <hash> --sha256-arm64 <hash>" >&2
    exit 1
}

if [ $# -lt 1 ]; then
    usage
fi

VERSION="$1"
SHA256_X64=""
SHA256_ARM64=""

shift
while [[ $# -gt 0 ]]; do
    case $1 in
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

if [[ -z "$SHA256_X64" || -z "$SHA256_ARM64" ]]; then
    echo "Error: Both --sha256-x64 and --sha256-arm64 are required" >&2
    usage
fi

cat << EOF
# typed: false
# frozen_string_literal: true

class SudoMcp < Formula
  desc "MCP server allowing AI models to execute privileged commands via sudo"
  homepage "https://github.com/hughesjs/sudo-mcp"
  version "${VERSION}"
  license "MIT"

  on_macos do
    if Hardware::CPU.intel?
      url "https://github.com/hughesjs/sudo-mcp/releases/download/v#{version}/sudo-mcp-macos-x64-v#{version}.tar.gz"
      sha256 "${SHA256_X64}"
    end
    if Hardware::CPU.arm?
      url "https://github.com/hughesjs/sudo-mcp/releases/download/v#{version}/sudo-mcp-macos-arm64-v#{version}.tar.gz"
      sha256 "${SHA256_ARM64}"
    end
  end

  def install
    bin.install "sudo-mcp"
  end

  def post_install
    (var/"log/sudo-mcp").mkpath
  end

  def caveats
    <<~EOS
      ⚠️  SECURITY WARNING: sudo-mcp gives AI models root access to your system.

      This is an inherently dangerous tool. By design, it allows an AI model to
      execute commands with root privileges. USE AT YOUR OWN RISK.

      Audit logs are written to: ~/Library/Logs/sudo-mcp/audit.log

      Configure with Claude Code:
        claude mcp add sudo-mcp #{opt_bin}/sudo-mcp

      See SECURITY.md for full details: https://github.com/hughesjs/sudo-mcp/blob/master/SECURITY.md
    EOS
  end

  test do
    assert_match "sudo-mcp", shell_output("#{bin}/sudo-mcp --help")
  end
end
EOF
