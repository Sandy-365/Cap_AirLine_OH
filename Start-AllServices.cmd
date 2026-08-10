@echo off
title Airline Management System Services
echo Starting all services in a single CMD window...
powershell -ExecutionPolicy Bypass -File "%~dp0Start-AllServices.ps1"
