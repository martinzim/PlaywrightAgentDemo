FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/AiBrowserTester/AiBrowserTester.csproj", "src/AiBrowserTester/"]
COPY ["src/Demo.ServiceDefaults/Demo.ServiceDefaults.csproj", "src/Demo.ServiceDefaults/"]
RUN dotnet restore "src/AiBrowserTester/AiBrowserTester.csproj"
COPY . .
RUN dotnet publish "src/AiBrowserTester/AiBrowserTester.csproj" -c Release -r linux-x64 --self-contained true -o /app/publish
FROM mcr.microsoft.com/playwright/dotnet:v1.58.0-noble AS final
WORKDIR /app
ENV Browser__Name=chromium
ENV Browser__Headed=false
ENV Artifacts__OutputPath=/artifacts
COPY --from=build /app/publish .
ENTRYPOINT ["/app/AiBrowserTester"]
