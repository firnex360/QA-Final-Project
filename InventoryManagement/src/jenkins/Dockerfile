FROM jenkins/jenkins:lts-jdk21

USER root

# 1. Install prerequisites for Docker CLI, .NET SDK, and ICU Globalization
RUN apt-get update && apt-get install -y \
    curl \
    git \
    wget \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    libicu-dev \
    libgssapi-krb5-2 \
    libssl3 \
    zlib1g \
    && rm -rf /var/lib/apt/lists/*

# 2. Install Docker CLI (so Jenkins can build & run containers)
RUN mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update && apt-get install -y docker-ce-cli \
    && rm -rf /var/lib/apt/lists/*

# 3. Install .NET 10 SDK & Link to System Binaries
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir /usr/share/dotnet \
    && chmod -R 755 /usr/share/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /bin/dotnet

# 4. Set Environment Variables
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="/usr/share/dotnet:${PATH}:/var/jenkins_home/.dotnet/tools"

USER jenkins
