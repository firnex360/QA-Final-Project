FROM jenkins/jenkins:lts-jdk21

USER root

# 1. Install prerequisites for Docker CLI & .NET runtime dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
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
    && apt-get update && apt-get install -y docker-ce-cli --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# 3. Install Node.js (for Newman / Postman API testing)
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# 4. Copy .NET 10 SDK directly from Microsoft's official image (no download needed)
COPY --from=mcr.microsoft.com/dotnet/sdk:10.0 /usr/share/dotnet /usr/share/dotnet
RUN ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet

# 5. Set environment variables for .NET & Global Tools
ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="/usr/share/dotnet:${PATH}:/var/jenkins_home/.dotnet/tools"

USER jenkins
