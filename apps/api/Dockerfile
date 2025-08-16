FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl

# Copy project file
COPY *.csproj ./

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Expose port
EXPOSE 5653

# Run with hot reload for development
CMD ["dotnet", "watch", "run", "--urls", "http://+:5653"]