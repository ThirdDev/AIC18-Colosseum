FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src
COPY *.sln ./
COPY Colosseum.OnTheOcean/Colosseum.OnTheOcean.csproj Colosseum.OnTheOcean/
RUN dotnet restore
COPY . .
WORKDIR /src/Colosseum.OnTheOcean
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM sgrio/java-oracle:jre_8 as java

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENV JAVA_HOME /usr/lib/jvm/java-8-oracle
ENV JRE_HOME ${JAVA_HOME}
COPY --from=java ${JAVA_HOME} ${JAVA_HOME}
RUN update-alternatives --install "/usr/bin/java" "java" "${JRE_HOME}/bin/java" 1 && \
	update-alternatives --install "/usr/bin/javaws" "javaws" "${JRE_HOME}/bin/javaws" 1 && \
	update-alternatives --set java "${JAVA_HOME}/bin/java" && \
	update-alternatives --set javaws "${JAVA_HOME}/bin/javaws"
ENTRYPOINT ["dotnet", "Colosseum.OnTheOcean.dll"]
