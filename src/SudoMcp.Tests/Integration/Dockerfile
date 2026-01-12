# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/SudoMcp/SudoMcp.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -o /app/publish \
    /p:PublishSingleFile=true

# Stage 2: Runtime
FROM debian:bookworm-slim

# Install runtime dependencies
RUN apt-get update && apt-get install -y \
    policykit-1 \
    sudo \
    dbus \
    libicu72 \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Create test user with NOPASSWD sudo
RUN useradd -m -s /bin/bash testuser && \
    echo "testuser ALL=(ALL) NOPASSWD: ALL" > /etc/sudoers.d/testuser && \
    chmod 0440 /etc/sudoers.d/testuser

# Configure polkit for passwordless pkexec for testuser
RUN mkdir -p /etc/polkit-1/rules.d && \
    echo 'polkit.addRule(function(action, subject) {' > /etc/polkit-1/rules.d/50-sudo-mcp-test.rules && \
    echo '    if (subject.user == "testuser" && action.id == "org.freedesktop.policykit.exec") {' >> /etc/polkit-1/rules.d/50-sudo-mcp-test.rules && \
    echo '        return polkit.Result.YES;' >> /etc/polkit-1/rules.d/50-sudo-mcp-test.rules && \
    echo '    }' >> /etc/polkit-1/rules.d/50-sudo-mcp-test.rules && \
    echo '});' >> /etc/polkit-1/rules.d/50-sudo-mcp-test.rules

# Copy binary and install script to staging directory
WORKDIR /tmp/install
COPY --from=build /app/publish/SudoMcp ./sudo-mcp
COPY scripts/install.sh ./install.sh
RUN chmod +x ./sudo-mcp ./install.sh

# Run install script
RUN ./install.sh

# Create entrypoint to start dbus
RUN printf '#!/bin/bash\nmkdir -p /run/dbus\ndbus-daemon --system --fork\nsleep 0.5\nexec "$@"\n' > /entrypoint.sh && \
    chmod +x /entrypoint.sh

# Fix log directory ownership for testuser
RUN chown testuser:testuser /var/log/sudo-mcp

# Cleanup staging directory
RUN rm -rf /tmp/install

WORKDIR /
ENTRYPOINT ["/entrypoint.sh"]
CMD ["sleep", "infinity"]