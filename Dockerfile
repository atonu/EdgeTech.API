FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY EdgeTech.API.csproj .
RUN dotnet restore EdgeTech.API.csproj
COPY . .
RUN dotnet publish EdgeTech.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
EXPOSE 5001
ENTRYPOINT ["dotnet", "EdgeTech.API.dll"]
