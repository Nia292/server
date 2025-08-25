@echo off
docker build -t syrilai/sinus-synchronous-services:latest . -f ..\Dockerfile-SinusSynchronousServices --no-cache --pull --force-rm