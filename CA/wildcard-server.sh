#!/bin/sh

openssl req -config wildcard-server.conf -days 3650 -newkey rsa:4096 -keyout wildcard-server.key -out wildcard-server.req

openssl ca -batch -config openssl.conf -cert Hamiller-Tube-IM.pem -keyfile Hamiller-Tube-IM.key -key monkey -extfile wildcard-server.conf -extensions wildcard_server_exts -out wildcard-server.pem -in wildcard-server.req

openssl pkcs12 -export -passout pass:monkey -out wildcard-server-bare.pfx -inkey wildcard-server.key -in wildcard-server.pem
openssl pkcs12 -export -passout pass:monkey -out wildcard-server.pfx -inkey wildcard-server.key -in wildcard-server.pem -certfile Hamiller-Tube-IM.pem
openssl pkcs12 -export -passout pass:monkey -out wildcard-server-full.pfx -inkey wildcard-server.key -in wildcard-server.pem -certfile Hamiller-Tube-IM-and-CA.pem

openssl x509 -in wildcard-server.pem -text > wildcard-server.cert
openssl rsa -in wildcard-server.key -passin pass:monkey -text >> wildcard-server.cert 

