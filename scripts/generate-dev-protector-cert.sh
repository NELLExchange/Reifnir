#!/bin/bash

# Prompt for password (hidden input)
read -s -p "Enter password for certificate: " password
echo

# Prompt for output path with default
output_path_default="$HOME/.nellebot-certs/dev-protector.pfx"
read -p "Enter certificate output path [$output_path_default]: " output_path

# Use default if empty
if [ -z "$output_path" ]; then
    output_path="$output_path_default"
fi

# Check if file already exists
if [ -f "$output_path" ]; then
    echo "Error: Certificate output path already exists: $output_path" >&2
    exit 1
fi

# Create output directory if it doesn't exist
mkdir -p "$(dirname "$output_path")"

# Calculate validity dates (10 years)
not_before=$(date +%Y%m%d%H%M%SZ)
not_after=$(date -d "+10 years" +%Y%m%d%H%M%SZ)

# Generate temporary files for key and certificate
temp_key=$(mktemp)
temp_cert=$(mktemp)

# Cleanup temp files on exit
trap "rm -f $temp_key $temp_cert" EXIT

# Generate RSA 2048-bit private key
openssl genrsa -out "$temp_key" 2048 2>/dev/null

# Create self-signed certificate (valid for 10 years)
openssl req -new -x509 \
    -key "$temp_key" \
    -out "$temp_cert" \
    -days 3650 \
    -subj "/CN=DEV_PROTECTOR_CERT" \
    -sha256 2>/dev/null

# Export to PFX/PKCS12 format with password
openssl pkcs12 -export \
    -inkey "$temp_key" \
    -in "$temp_cert" \
    -out "$output_path" \
    -passout "pass:$password" \
    -name "DEV_PROTECTOR_CERT"

# Calculate and display thumbprint (SHA1 fingerprint)
thumbprint=$(openssl x509 -in "$temp_cert" -noout -fingerprint -sha1 | cut -d= -f2 | tr -d :)

echo "Exported certificate with thumbprint: $thumbprint"
