#!/bin/bash

dotnet build

cd TokiwadaiPride.Database; dotnet run & cd ../
docker run -p 6379:6379 redis &
cd TokiwadaiPride.Bot; dotnet run & cd ../
cd TokiwadaiPride.Web; dotnet run & cd ../
