#!/bin/sh
docker build -t syrilai/sinus-synchronous-staticfilesserver:latest . -f ../Dockerfile-SinusSynchronousStaticFilesServer-git --no-cache --pull --force-rm