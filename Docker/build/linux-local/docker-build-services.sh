#!/bin/sh
cd ../../../
docker build -t syrilai/sinus-synchronous-services:latest . -f ../Dockerfile-SinusSynchronousServices --no-cache --pull --force-rm
cd Docker/build/linux-local