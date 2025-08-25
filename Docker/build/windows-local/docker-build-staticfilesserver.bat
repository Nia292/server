@echo off
docker build -t syrilai/sinus-synchronous-staticfilesserver:latest . -f ..\Dockerfile-SinusSynchronousStaticFilesServer --no-cache --pull --force-rm