#!/bin/bash

# Script untuk renew certificate Let's Encrypt dan restart container

echo "Starting certificate renewal process..."

# Stop container sementara
cd /home/ubuntu/Sunrise
docker compose down

# Renew certificate
sudo certbot renew --quiet

# Convert certificate baru ke PFX (multi-domain)
sudo openssl pkcs12 -export -out sunrise-multi-domain.pfx -inkey /etc/letsencrypt/live/api.tosume.me/privkey.pem -in /etc/letsencrypt/live/api.tosume.me/cert.pem -password pass:password

# Copy dan set permission
sudo cp sunrise-multi-domain.pfx certificate.pfx
sudo chown ubuntu:docker certificate.pfx

# Restart container
docker compose up -d

echo "Certificate renewal completed!" 