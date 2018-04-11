FROM microsoft/dotnet:2.0.5-sdk-2.1.4 as builder  
 
RUN mkdir -p /root/src/app/HueBridge
WORKDIR /root/src/app/HueBridge

#copy just the project file over
# this prevents additional extraneous restores
# and allows us to re-use the intermediate layer
# This only happens again if we change the csproj.
# This means WAY faster builds!
COPY HueBridge.csproj .
#Because we have a custom nuget.config, copy it in
# COPY nuget.config . 
RUN dotnet restore ./HueBridge.csproj 

COPY . .
RUN dotnet publish -c release -o published -r linux-arm 

#Smaller - Best for apps with self-contained .NETs, as it doesn't include the runtime
#          It has the *dependencies* to run .NET Apps. The .NET runtime image sits on this
# FROM microsoft/dotnet:2.0.0-runtime-deps-stretch-arm32v7 

#Bigger - Best for apps .NETs that aren't self-contained.
FROM microsoft/dotnet:2.0.5-runtime-stretch-arm32v7

# These are the non-ARM images. 
#FROM microsoft/dotnet:2.0.0-runtime-deps
#FROM microsoft/dotnet:2.0.0-runtime

WORKDIR /root/
COPY --from=builder /root/src/app/HueBridge/published .
ENV ASPNETCORE_URLS=http://+:80
# This runs your app with the dotnet exe included with the runtime or SDK
CMD ["dotnet", "./HueBridge.dll"]  
# This runs your self-contained .NET Core app. You built with -r to get this
# CMD ["./HueBridge"] 