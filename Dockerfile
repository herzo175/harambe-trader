FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

WORKDIR /app

ADD bin/build/netcoreapp3.1/publish/ .
ADD strategy.yml .

ENTRYPOINT [ "dotnet", "harambe-trader.dll" ]
