# Docker Projects for Nx Witness and Nx Meta and DW Spectrum

This is a project to build docker containers for [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/), and [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness, and [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

## License

![GitHub License](https://img.shields.io/github/license/ptr727/NxWitness)

## Build Status

[Code and Pipeline is on GitHub](https://github.com/ptr727/NxWitness):  
![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/NxWitness?logo=github)  
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/ptr727/NxWitness/Build%20and%20Publish%20Docker%20Images?logo=github)

## Container Images

Docker container images are published on [Docker Hub](https://hub.docker.com/u/ptr727).  
Images are tagged using `latest` or `stable` and the specific build version number.  
`latest` images use the latest patch release version.  
`stable` images use the last stable release version.  
This allows flexibility for deployments that want to pin to specifc release channels or versions.

Images are automatically rebuilt every Monday morning, picking up the latest base image fixes.

[NxWitness](https://hub.docker.com/r/ptr727/nxwitness)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness/stable?label=stable&logo=docker)

[NxWitness-LSIO](https://hub.docker.com/r/ptr727/nxwitness-lsio)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/stable?label=stable&logo=docker)

[NxMeta](https://hub.docker.com/r/ptr727/nxmeta)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta/stable?label=stable&logo=docker)

[NxMeta-LSIO](https://hub.docker.com/r/ptr727/nxmeta-lsio)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/stable?label=stable&logo=docker)

[DWSpectrum](https://hub.docker.com/r/ptr727/dwspectrum)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum/stable?label=stable&logo=docker)

[DWSpectrum-LSIO](https://hub.docker.com/r/ptr727/dwspectrum-lsio)  
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/latest?label=latest&logo=docker)
![Docker Image Version](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/stable?label=stable&logo=docker)

## Overview

### Introduction

My initial inspiration to convert my DW Spectrum system running on a VM to docker came from [The Home Repot NxWitness](https://github.com/thehomerepot/nxwitness) project. I started with one GitHub docker project repository, then two, then four, and by five I decided to consolidate the various, but very similar, repositories into a single project.  

The Network Optix reference docker project is located [here](https://github.com/networkoptix/nx_open_integrations/tree/master/docker).  

The biggest outstanding docker challenges are hardware bound licensing, and lack of admin defined storage locations.

### Products

The project supports three product variants:

- [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/).
- [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness.
- [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

### Base Images

The project create two variants of each product using different base images:

- [Ubuntu](https://ubuntu.com/) using [ubuntu:bionic](https://hub.docker.com/_/ubuntu) base image.
- [LinuxServer](https://www.linuxserver.io/) using [lsiobase/ubuntu:bionic](https://hub.docker.com/r/lsiobase/ubuntu) base image.

Note, I can use smaller base images like [alpine](https://hub.docker.com/_/alpine), but the mediaserver officially [supports](https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support) Ubuntu Bionic.

### LinuxServer

The [LinuxServer (LSIO)](https://www.linuxserver.io/) base images provide valuable functionality:

- The LSIO images are based on [s6-overlay](https://github.com/just-containers/s6-overlay), and LSIO [produces](https://fleet.linuxserver.io/) containers for many popular open source apps.
- LSIO allows us to [specify](https://docs.linuxserver.io/general/understanding-puid-and-pgid) the user account to use when running the container process.
- This is [desired](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user) if we do not want to run as root, or required if we need user specific permissions when accessing mapped volumes.
- We could achieve a similar outcome by using Docker's [--user](https://docs.docker.com/engine/reference/run/#user) option, but it is often more convenient to modify environment variables vs. controlling how a container runs.

## Configuration

The docker configuration is reasonably simple, requiring just two volume mappings for configuration files and media storage.

### Volumes

`/config` : Configuration files.  
`/media` : Recording files.  
`/archive` : Backup files. (Optional)

The Nx Witness backup implementation is [not very useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy), as it only makes a copy of the recordings, it does not extend the retention period.

The mediaserver filters [filesystems](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#notes-about-storage) by type, and the `/media` mapping must point to a supported filesytem.  
ZFS and Unraid's FUSE filesystems are by default not supported, and requires the advanced `additionalLocalFsTypes` setting to be configured, see the notes section for details.  
An alternatively for Unraid is to use the Unassigned Devices plugin and assign storage to e.g. a XFS formatted SSD drive.

### Ports

`7001` : Default server port.

### Environment Variables

`PUID` : User Id (LSIO only, see [docs](https://docs.linuxserver.io/general/understanding-puid-and-pgid) for usage).  
`PGID` : Group Id (LSIO only).  
`TZ` : Timezone, e.g. `Americas/Los_Angeles`.

### Network Mode

Any network mode can be used, but due to the hardware bound licensing, `host` mode is [preferred](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#networking).

## Examples

### Docker Create

```console
docker create \
  --name=dwspectrum-lsio-test-container \
  --hostname=dwspectrum-lsio-test-host \
  --domainname=foo.bar.net \
  --restart=unless-stopped \
  --network=host \
  --env TZ=Americas/Los_Angeles \
  --volume /mnt/dwspectrum/config:/config:rw \
  --volume /mnt/dwspectrum/media:/media:rw \
  ptr727/dwspectrum-lsio

docker start dwspectrum-lsio-test-container
```

### Docker Compose

```yaml
version: "3.7"

services:
  dwspectrum:
    image: ptr727/dwspectrum-lsio
    container_name: dwspectrum-lsio-test-container
    restart: unless-stopped
    network_mode: host
    environment:
      - TZ=Americas/Los_Angeles
    volumes:
      - /mnt/dwspectrum/config:/config
      - /mnt/dwspectrum/media:/media
```

### Unraid Template

- Add the template [URL](./Unraid) `https://github.com/ptr727/NxWitness/tree/master/Unraid` to the "Template Repositories" section, at the bottom of the "Docker" configuration tab, and click "Save".
- Create a new container by clicking the "Add Container" button, select the desired product template from the dropdown.
- If using Unassigned Devices for media storage, use `RW/Slave` access mode.
- Use `nobody` and `users` identifiers, `PUID=99` and `PGID=100`.

## Product Releases

Product releases and updates can be found at the following locations:

- Nx Witness:
  - [JSON API](https://nxvms.com/api/utils/downloads)
  - [Nx Witness Downloads](https://nxvms.com/download/linux)
  - [Nx Witness Beta Downloads](https://beta.networkoptix.com/beta-builds/default)
- Nx Meta:
  - [JSON API](https://meta.nxvms.com/api/utils/downloads)
  - [Nx Meta Early Access Signup](https://support.networkoptix.com/hc/en-us/articles/360046713714-Get-an-Nx-Meta-Build)
  - [Nx Meta Downloads](https://meta.nxvms.com/download/linux)
  - [Nx Meta Beta Downloads](https://meta.nxvms.com/downloads/patches)
- DW Spectrum:
  - [JSON API](https://dwspectrum.digital-watchdog.com/api/utils/downloads)
  - [DW Spectrum Downloads](https://dwspectrum.digital-watchdog.com/download/linux)
  - The latest DW Spectrum versions are often not listed, but sometimes do match the same version as used by Nx Witness.
  - Use the latest NX Witness URL, and substitute the "default" string for "digitalwatchdog", e.g.:
    - [https://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb](https://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb)
    - [https://updates.networkoptix.com/digitalwatchdog/30917/linux/dwspectrum-server-4.0.0.30917-linux64.deb](https://updates.networkoptix.com/digitalwatchdog/30917/linux/dwspectrum-server-4.0.0.30917-linux64.deb)

## Build Process

With three products and two base images we end up with six different dockerfiles, that all basically look the same. Unfortunately Docker does [not support](https://github.com/moby/moby/issues/735) an `include` directive, so I [use](http://bobbynorton.com/posts/includes-in-dockerfiles-with-m4-and-make/) a `Makefile` to dynamically create a `Dockerfile` for every variant.

I converted the build from Docker Hub automated builds to using GitHub Actions. I have yet to implement triggers that update images when the upstream images are updated, for now I build weekly picking up the latest upstream changes and applying patches.

I should also figure out how to automatically detect newly released versions, and automatically rebuild when a new version is detected. This could possibly be done using page change notifiers and webhooks, with some code to extract the relevant version and URL attributes from the web pages. For now I'm manually updating as I notice the versions changing.

## Network Optix and Docker

### Issues

The biggest outstanding docker challenges are hardware bound licensing, and lack of admin defined storage locations.

The camera license keys are activated using hardware attributes of the server, that is not docker friendly, and occasionally results in activation failures. I would much prefer a modern approach to apply licenses to my cloud account, allowing me to run on whatever hardware I want. Living in the US, I have to buy my licenses from [Digital Watchdog](https://digital-watchdog.com/), and in my experience their license enforcement policy is "draconian", three activations and you have to buy a new license. That really means that the [Lifetime Upgrades and No Annual Agreements](https://digital-watchdog.com/dws/upgrades/) license is the lifetime of the hardware on which the license was activated. So let's say hardware is replaced every two years, three activations, lifetime is about six years, not much of a lifetime compared to other products that are license flexible but require maintenance or renewals.

As for storage, the mediaserver attempts to automatically decide what storage to use, applies filesystem type and instance filtering, and blindly creates files on any storage it deems fit. This overly complicated logic does not work in docker, and a much simpler and reliable approach would be to allow an admin to be in control and specify exactly what storage to be used. I really do not understand what the reasons were for building complicated decision logic, instead of being like all other servers that use storage and let the admin define the storage locations.

All in, Nx Witness is in my experience still the lightest on resources with good features VMS/NVR I've used, and with docker support, is great to run in my home lab.

### Wishlist

My wishlist for better [docker support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker):

- Publish always up to date and ready to use docker images on Docker Hub.
- Use the cloud account for license enforcement.
- Allow the administrator to specify and use any storage location.
- Allow the administrator to specify the bound network adapter.
- Implement a [more useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy) recording archive management system, allowing for separate high speed recording, and high capacity playback storage volumes.

## Notes

- Version 4.1+ includes the ability to specify additional storage filesystem types. This is particularly useful when running on Unraid or ZFS storage that is by default not supported.
  - Access the server storage page at `http://hostname:7001/static/index.html#/info` and verify that all mounted storage is listed.
  - If storage is not listed, attach to the container console and run `cat /proc/mounts` to get a list of all the mounted filesystem types.
  - Access the advanced settings page at `http://hostname:7001/static/index.html#/advanced` and set `additionalLocalFsTypes` to include the filesystem type.
  - Add `fuse.grpcfuse` for Docker for Windows, `fuse.shfs` for Unraid, and `zfs` for ZFS, e.g. `fuse.grpcfuse,fuse.shfs,zfs`.
  - Save the settings, restart the server, and verify that storage is now available.
  - Alternative call the API directly via `http://admin:<passsword>@hostname:7001/api/systemSettings?additionalLocalFsTypes=fuse.grpcfuse,fuse.shfs`.
- There is no way to configure the `additionalLocalFsTypes` types at deployment time.
  - Some debugging shows the setting is stored in the `var/ecs.sqlite` DB file, in the `vms_kvpair` table, `name=additionalLocalFsTypes`, `value=fuse.grpcfuse,fuse.shfs,zfs`.
  - This DB table contains lots of other information, so it seems unfeasible to pre-seed the system with this DB file, and modifying it at runtime is as complex as calling the web service.
- The [calculation](http://mywiki.wooledge.org/BashFAQ/028) of `VMS_DIR=$(dirname $(dirname "${BASH_SOURCE[0]}"))` in `../bin/mediaserver` can result in bad paths when called from the same directory, e.g. `start-stop-daemon: unable to stat ./bin/./bin/mediaserver-bin (No such file or directory)`.
- The filesystem filter logic incorrectly considers some volumes to be duplicates, turn on verbose logging (`logLevel=DEBUG2`). `VERBOSE nx::vms::server::fs: shfs /archive fuse.shfs - duplicate`.
- The mediaserver pollutes the filesystem by blindly creating a `Nx MetaVMS Media` folder and DB files in any storage it finds.
- The mediaserver will bind to any network adapter it discovers, including virtual adapters used by other containers. There is no way to disable auto binding. All the bound network adapters are displayed in the performance graph, and makes it near impossible to use due.
- The download CDN SSL certificates are not trusted on all systems, and we need to disable certificate checks when using HTTPS for downloads. `ERROR: cannot verify updates.networkoptix.com's certificate, issued by 'CN=Amazon,OU=Server CA 1B,O=Amazon,C=US': Unable to locally verify the issuer's authority. To connect to updates.networkoptix.com insecurely, use --no-check-certificate`
- Windows Subsystem for Linux v2 (WSL2) is not supported.
  - In the DEB installer `postinst` step the installer tries to start the service, and fails the install. `Detected runtime type: wsl.`, `System has not been booted with systemd as init system (PID 1). Can't operate.`
  - The logic tests for `if [[ $RUNTIME != "docker" ]]`, while the runtime reported by WSL2 is `wsl`.
  - The logic [should](https://support.networkoptix.com/hc/en-us/community/posts/1500000699041-WSL2-docker-runtime-not-supported) perform a `systemd` positive test vs. testing for not docker.
- Version 4.3+ remove the shell scripts that used to launch the binary files.
  - The old shell script `mediaserver` is now what used to be `mediaserver-bin`, and `root-tool` is now what used to be `root-tool-bin`.
  - The Nx Docker project launch [script](https://github.com/networkoptix/nx_open_integrations/blob/master/docker/entrypoint.sh), is still referring to the old names.
  - The S6 services config was changed to launch the `foo-bin` variants if they exist, else just `foo`.
