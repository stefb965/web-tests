#!/bin/sh

openssl req -config duplicate-hash-invalid-ca.conf -new -newkey rsa:4096 -x509 -sha256 -days 3650 -keyout duplicate-hash-invalid-ca.key -passout pass:monkey -out duplicate-hash-invalid-ca.pem 

