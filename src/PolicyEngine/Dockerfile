FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN cd policy-engine
RUN dotnet restore
RUN dotnet publish -o out
FROM mcr.microsoft.com/dotnet/aspnet:8.0	
WORKDIR /app
COPY --from=build /app/out /app
ENTRYPOINT [ "dotnet", "policy-engine.dll" ]