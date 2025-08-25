#!/bin/sh
cd ../../../
docker build -t syrilai/sinus-synchronous-server:latest . -f ../Dockerfile-SinusSynchronousServer --no-cache --pull --force-rm
cd Docker/build/linux-local