#!/bin/sh

echo '100001' > duplicate-hash-serial
cp /dev/null duplicate-hash-certindex.txt
rm -f duplicate-hash-certs/*
mkdir -p duplicate-hash-certs

openssl req -config duplicate-hash-ca.conf -new -newkey rsa:4096 -x509 -sha256 -days 3650 -keyout duplicate-hash-ca.key -passout pass:monkey -out duplicate-hash-ca.pem 

