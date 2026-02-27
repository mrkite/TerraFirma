# Terrafirma

## Dependencies (Debian/Ubuntu)

```sh
sudo apt install build-essential cmake pkg-config \
  libx11-dev libxext-dev libxrandr-dev libxcursor-dev libxfixes-dev \
  libxi-dev libxss-dev libxtst-dev libxkbcommon-dev \
  libwayland-dev libdecor-0-dev \
  libgl1-mesa-dev libegl1-mesa-dev \
  libdrm-dev libgbm-dev
```

SDL3 is built from the vendored submodule — no system SDL package needed.

## Build

```sh
git submodule update --init --recursive
cmake -B build
cmake --build build --parallel "$(nproc)"
```

## Install

```sh
cmake --install build --prefix /usr/local
```

## Debug build

```sh
cmake -B build -DCMAKE_BUILD_TYPE=Debug
cmake --build build --parallel "$(nproc)"
```
