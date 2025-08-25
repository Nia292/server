#!/bin/sh
docker build -t syrilai/sinus-synchronous-authservice:latest . -f ../Dockerfile-SinusSynchronousAuthService-git --no-cache --pull --force-rm