#!/bin/sh
cd ../../../
docker build -t syrilai/sinus-synchronous-staticfilesserver:latest . -f ../Dockerfile-SinusSynchronousStaticFilesServer --no-cache --pull --force-rm
cd Docker/build/linux-local