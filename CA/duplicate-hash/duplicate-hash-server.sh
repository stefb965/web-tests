#!/bin/sh

openssl req -config duplicate-hash-server.conf -days 3650 -newkey rsa:4096 -keyout duplicate-hash-server.key -out duplicate-hash-server.req

openssl ca -batch -config duplicate-hash-ca.conf -cert duplicate-hash-ca.pem -keyfile duplicate-hash-ca.key -key monkey -extfile duplicate-hash-server.conf -extensions server_exts -out duplicate-hash-server.pem -in duplicate-hash-server.req 

openssl pkcs12 -export -passout pass:monkey -out duplicate-hash-server.pfx -inkey duplicate-hash-server.key -in duplicate-hash-server.pem

openssl x509 -in duplicate-hash-server.pem -text > duplicate-hash-server.cert
openssl rsa -in duplicate-hash-server.key -text >> duplicate-hash-server.cert 

openssl pkcs12 -export -passout pass:monkey -out duplicate-hash-server-full.pfx -inkey duplicate-hash-server.key -in duplicate-hash-server.pem -certfile duplicate-hash-ca.pem

