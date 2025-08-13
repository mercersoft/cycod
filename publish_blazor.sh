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
  --exclude 'cs/' --exclude 'de/' --exclude 'es/' --exclude 'fr/' \
  --exclude 'it/' --exclude 'ja/' --exclude 'ko/' --exclude 'pl/' \
  --exclude 'pt-BR/' --exclude 'ru/' --exclude 'tr/' --exclude 'zh-Hans/' \
  --exclude 'zh-Hant/' --exclude '*Test*' --exclude '*.resources.*' \
  .blazor-publish/wwwroot/_framework/ web/public/_framework/