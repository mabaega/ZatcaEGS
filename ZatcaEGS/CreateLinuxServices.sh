#!/bin/bash

# Zatca EGS Runner Script

# Fungsi untuk memeriksa apakah .NET runtime terinstal
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo ".NET runtime tidak ditemukan. Silakan instal .NET runtime terlebih dahulu."
        echo "Kunjungi https://dotnet.microsoft.com/download/dotnet untuk panduan instalasi."
        exit 1
    fi
}

# Fungsi untuk memberikan izin eksekusi
set_permissions() {
    chmod +x ZatcaEGS
    echo "Izin eksekusi diberikan ke ZatcaEGS"
}

# Fungsi untuk menjalankan aplikasi
run_app() {
    if [ "$1" == "service" ]; then
        echo "Menjalankan ZatcaEGS sebagai layanan..."
        sudo ./ZatcaEGS
    else
        echo "Menjalankan ZatcaEGS dalam mode interaktif..."
        ./ZatcaEGS
    fi
}

# Fungsi untuk membuat dan mengaktifkan layanan systemd
setup_service() {
    echo "Membuat file layanan systemd untuk ZatcaEGS..."
    sudo tee /etc/systemd/system/zatca-egs.service > /dev/null << EOL
[Unit]
Description=Zatca EGS Service
After=network.target

[Service]
ExecStart=$(pwd)/ZatcaEGS
WorkingDirectory=$(pwd)
User=$(whoami)
Restart=always

[Install]
WantedBy=multi-user.target
EOL

    echo "Mengaktifkan dan memulai layanan ZatcaEGS..."
    sudo systemctl enable zatca-egs.service
    sudo systemctl start zatca-egs.service
    echo "Layanan ZatcaEGS telah diaktifkan dan dijalankan."
}

# Main script
echo "Selamat datang di ZatcaEGS Runner"

# Periksa .NET runtime
check_dotnet

# Set izin
set_permissions

# Tanyakan pengguna apakah ingin menjalankan sebagai layanan atau interaktif
read -p "Apakah Anda ingin menjalankan ZatcaEGS sebagai layanan sistem? (y/n): " run_as_service

if [ "$run_as_service" == "y" ] || [ "$run_as_service" == "Y" ]; then
    setup_service
else
    run_app
fi

echo "Terima kasih telah menggunakan ZatcaEGS Runner"
