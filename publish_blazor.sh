#!/bin/bash

# publish the blazor app with AOT and size optimizations
dotnet publish src/cycodblazor/cycodblazor.csproj -c Release \
  -p:DebuggerSupport=false \
  -p:LinkDuringPublish=true \
  -p:InvariantGlobalization=true \
  -p:BlazorEnableCompression=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=link \
  -o .blazor-publish

# clean target completely
rm -rf web/public/_framework
rm -rf web/public/blazor
rm -rf web/dist/blazor
rm -f web/public/*.staticwebassets.endpoints.json

# copy only essential runtime files for web deployment
mkdir -p web/public/_framework
rsync -a --delete --prune-empty-dirs \
  --exclude '*.br' --exclude '*.gz' --exclude '*.pdb' \
  --exclude '*.map' --exclude '*.xml' --exclude '*.symbols' \
  --exclude 'cs/' --exclude 'de/' --exclude 'es/' --exclude 'fr/' \
  --exclude 'it/' --exclude 'ja/' --exclude 'ko/' --exclude 'pl/' \
  --exclude 'pt-BR/' --exclude 'ru/' --exclude 'tr/' --exclude 'zh-Hans/' \
  --exclude 'zh-Hant/' --exclude '*Test*' --exclude '*.resources.*' \
  --exclude 'icudt_CJK.*' --exclude 'icudt_EFIGS.*' \
  --exclude 'lib/' --exclude 'samples/' \
  .blazor-publish/wwwroot/_framework/ web/public/_framework/

# copy essential static files only (exclude large JSON manifest)
rsync -a --delete --prune-empty-dirs \
  --exclude '_framework/' --exclude 'lib/' \
  --exclude '*.map' --exclude 'samples/' \
  --exclude '*.staticwebassets.endpoints.json' \
  .blazor-publish/wwwroot/ web/public/blazor/

# Remove large unnecessary files from public directory
rm -f web/public/cycodblazor.staticwebassets.endpoints.json
rm -f web/public/*.staticwebassets.endpoints.json

# Show final size
echo "Final web deployment size:"
du -sh web/dist web/public