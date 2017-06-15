# Getting Started
* `git clone https://github.com/SignpostMarv/AjaxLife.git`
* `cd AjaxLife`
* `git submodule update --init --recursive`
* `cd server/assemblies/libopenmetaverse/`
  * run appropriate runprebuild for your platform/IDE
  * compile libopenmetaverse
* `cd server/assemblies/AjaxLife.Http/`
  * build assemblies/AjaxLife.Http/assemblies/bc-sharp/BouncyCastle.sln
  * `runprebuild.bat`
  * `compile.bat`
* `cd ../../`
* `runprebuild`
* `compile`

## Using HTTP2
* `npm install`
* `node http2-proxy.js`
