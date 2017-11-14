FROM microsoft/dotnet:2.0-sdk as builder
WORKDIR /app
COPY . .
RUN cd DockerLibUsb && dotnet restore && dotnet publish -c Release -r linux-arm -o /app/dist

FROM microsoft/dotnet:2.0-runtime-stretch-arm32v7
ENTRYPOINT ["dotnet", "DockerLibUsb.dll"]
WORKDIR /app
COPY --from=builder app/dist ./
RUN apt-get update
RUN apt-get install -y libusb-1.0-0-dev usbutils
RUN ln -s /lib/arm-linux-gnueabihf/libusb-1.0.so.0 libusb-1.0.dll