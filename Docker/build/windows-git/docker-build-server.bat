@echo off
docker build -t syrilai/sinus-synchronous-server:latest . -f ..\Dockerfile-SinusSynchronousServer-git --no-cache --pull --force-rm