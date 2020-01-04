# -*- mode: ruby -*-
# vi: set ft=ruby :

$script = <<-SCRIPT
echo I am provisioning...
echo Updating system
sudo apt update -y && sudo apt upgrade -y
echo Installing dotnet

wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update -y
sudo apt-get install apt-transport-https -y
sudo apt-get update -y
sudo apt-get install dotnet-sdk-3.1 -y
SCRIPT

Vagrant.configure("2") do |config|  

  config.vm.define "node1" do |node1|    
    node1.vm.box = "hashicorp/bionic64"
    node1.vm.network "public_network", ip: "192.168.50.11"
    node1.vm.provision "shell", inline: $script
  end

  config.vm.define "node2" do |node2|
    node2.vm.box = "hashicorp/bionic64"
    node2.vm.network "public_network", ip: "192.168.50.2"
    node2.vm.provision "shell", inline: $script
  end

  config.vm.define "node3" do |node3|
    node3.vm.box = "hashicorp/bionic64"
    node3.vm.network "public_network", ip: "192.168.50.3"
    node3.vm.provision "shell", inline: $script
  end

  config.vm.define "node4" do |node4|
    node4.vm.box = "hashicorp/bionic64"
    node4.vm.network "public_network", ip: "192.168.50.4"
    node4.vm.provision "shell", inline: $script
  end

  config.vm.define "node5" do |node5|
    node5.vm.box = "hashicorp/bionic64"
    node5.vm.network "public_network", ip: "192.168.50.5"
    node5.vm.provision "shell", inline: $script
  end
end
