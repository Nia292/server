@echo off
docker build -t syrilai/sinus-synchronous-services:latest . -f ..\Dockerfile-SinusSynchronousServices-git --no-cache --pull --force-rm