#!/bin/bash

# publish the blazor app
dotnet publish src/cycodblazor/cycodblazor.csproj -c Release \
  -p:DebuggerSupport=false \
  -p:LinkDuringPublish=true \
  -o .blazor-publish

# clean target
rm -rf web/public/_framework

# copy minimal runtime
rsync -a --delete --prune-empty-dirs \
  --exclude '*.br' --exclude '*.gz' --exclude '*.pdb' \
  .blazor-publish/wwwroot/_framework/ web/public/_framework/