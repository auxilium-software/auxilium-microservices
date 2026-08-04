# auxilium-microservices

## How to run on Debian 12
```
# Update packages and install prerequisites
sudo apt update
sudo apt install -y wget apt-transport-https software-properties-common

# Add Microsoft repo key
wget https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package list again
sudo apt update

# Install required components
sudo apt install -y dotnet-sdk-8.0
sudo apt install -y dotnet-runtime-8.0

# Get Repository
cd /opt
git clone https://github.com/auxilium-software/auxilium-microservices.git
cd /opt/auxilium-microservices/AuxiliumMicroservices
dotnet run --project AuxiliumMicroservices.csproj --config /opt/aux3-dev.yaml
```
