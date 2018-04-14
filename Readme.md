## Another hue bridge emulator (ASP.NET core implementation)

### The project simulates a Hue bridge 
### Currently supported devices
1. ESP8266 WiFi-enabled LED strips (https://github.com/mariusmotea/diyHue)
1. Konke WiFi-enabled outlets 

### To run the HueBridge on Raspberry Pi:
1. Build the application: ```dotnet publish -r linux-arm```, and plugins (e.g. ESP8266LightHandler) need to be built in the same way
1. Transfer the "published" folder to Raspberry Pi
1. ```chmod +x HueBridge```
1. Modify ```appsettings.json``` so that the IP address matches Pi's
1. Install package needed by dotnet runtime ```sudo apt-get install libunwind8```
1. Run the application ```./HueBridge```