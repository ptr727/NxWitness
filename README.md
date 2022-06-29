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
This allows flexibility for deployments that want to pin to specific release channels or versions.  
E.g.

```console
docker pull ptr727/nxwitness-lsio:latest
docker pull ptr727/nxwitness-lsio:stable
docker pull ptr727/nxwitness-lsio:4.2.0.33313
```

Images are automatically rebuilt every Monday morning, picking up the latest upstream updates.

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

The biggest outstanding docker challenges are hardware bound licensing and lack of admin defined storage locations.

### Products

The project supports three product variants:

- [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/).
- [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness.
- [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

### Base Images

The project creates two variants of each product using different base images:

- [Ubuntu](https://ubuntu.com/) using [ubuntu:focal](https://hub.docker.com/_/ubuntu) base image.
- [LinuxServer](https://www.linuxserver.io/) using [lsiobase/ubuntu:focal](https://hub.docker.com/r/lsiobase/ubuntu) base image.

Note, smaller base images, like [alpine](https://hub.docker.com/_/alpine), and the current Ubuntu 22.04 LTS (Jammy Jellyfish) are not [supported](https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support) by the mediaserver.

### LinuxServer

The [LinuxServer (LSIO)](https://www.linuxserver.io/) base images provide valuable functionality:

- The LSIO images are based on [s6-overlay](https://github.com/just-containers/s6-overlay), and LSIO [produces](https://fleet.linuxserver.io/) containers for many popular open source applications.
- LSIO allows us to [specify](https://docs.linuxserver.io/general/understanding-puid-and-pgid) the user account to use when running the container mediaserver process.
- This is [desired](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user) if we do not want to run as root, or required if we need user specific permissions when accessing mapped volumes.
- We could achieve a similar outcome by using Docker's [`--user`](https://docs.docker.com/engine/reference/run/#user) option, but the mediaserver's `root-tool` (used for license enforcement) requires running as `root`, thus the container must still be executed with `root` privileges, and we cannot use the `--user` option.
- The non-LSIO images do run the mediaserver as a non-root user, granting `sudo` rights to run the `root-tool` as `root`, but the user account (`${COMPANY_NAME}`) does not map to a user on the host system.

## Configuration

The docker configuration is reasonably simple, requiring just two volume mappings for configuration files and media storage.

### Volumes

`/config` : Configuration files.  
`/media` : Recording files.  
`/archive` : Backup files. (Optional)

Note, the video backup implementation is [not very useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy), as it only makes a copy of the recordings, it does not extend the retention period.

The mediaserver filters [filesystems](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#notes-about-storage) by type, and the `/media` mapping must point to a supported filesytem.  
ZFS, Unraid, and Docker Desktop filesystems are by default not supported, and requires the advanced `additionalLocalFsTypes` setting to be configured, see the notes section for details.  
An alternative for Unraid is to use the Unassigned Devices plugin and assign storage to e.g. a XFS formatted SSD drive.

### Ports

`7001` : Default server port.

### Environment Variables

`PUID` : User Id (LSIO only, see [docs](https://docs.linuxserver.io/general/understanding-puid-and-pgid) for usage).  
`PGID` : Group Id (LSIO only).  
`TZ` : Timezone, e.g. `Americas/Los_Angeles`.

### Network Mode

Any network mode can be used, but due to the hardware bound licensing, `host` mode is [recommended](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#networking).

## Examples

### Docker Create

```console
docker create \
  --name=nxwitness-lsio-test-container \
  --hostname=nxwitness-lsio-test-host \
  --domainname=foo.bar.net \
  --restart=unless-stopped \
  --network=host \
  --env TZ=Americas/Los_Angeles \
  --volume /mnt/nxwitness/config:/config:rw \
  --volume /mnt/nxwitness/media:/media:rw \
  ptr727/nxwitness-lsio:stable

docker start nxwitness-lsio-test-container
```

### Docker Compose

```yaml
version: "3.7"

services:
  nxwitness:
    image: ptr727/nxwitness-lsio:stable
    container_name: nxwitness-lsio-test-container
    restart: unless-stopped
    network_mode: host
    environment:
      - TZ=Americas/Los_Angeles
    volumes:
      - /mnt/nxwitness/config:/config
      - /mnt/nxwitness/media:/media
```

### Non-LSIO Docker Compose

The LSIO images re-link internal paths, while the non-LSIO images needs to map volumes directly to the installed folders.

```yaml
version: "3.7"

services:
  nxwitness:
    image: ptr727/nxwitness:stable
    container_name: nxwitness-test-container
    restart: unless-stopped
    network_mode: host
    volumes:
      - /mnt/nxwitness/config/etc:/opt/networkoptix/mediaserver/etc
      - /mnt/nxwitness/media:/opt/networkoptix/mediaserver/var/
```

### Unraid Template

- Add the template [URL](./Unraid) `https://github.com/ptr727/NxWitness/tree/master/Unraid` to the "Template Repositories" section, at the bottom of the "Docker" configuration tab, and click "Save".
- Create a new container by clicking the "Add Container" button, select the desired product template from the dropdown.
- If using Unassigned Devices for media storage, use `RW/Slave` access mode.
- Use `nobody` and `users` identifiers, `PUID=99` and `PGID=100`.

## Release Tracking

Product releases and updates can be found at the following locations:

- Nx Witness:
  - [JSON API](https://nxvms.com/api/utils/downloads)
  - [Nx Witness Downloads](https://nxvms.com/download/linux)
  - [Nx Witness Beta Downloads](https://beta.networkoptix.com/beta-builds/default)
- Nx Meta:
  - [JSON API](https://meta.nxvms.com/api/utils/downloads)
  - [Nx Meta Early Access Signup](https://support.networkoptix.com/hc/en-us/articles/360046713714-Get-an-Nx-Meta-Build)
  - [Nx Meta Request Developer Licenses](https://support.networkoptix.com/hc/en-us/articles/360045718294-Getting-Licenses-for-Developers)
  - [Nx Meta Downloads](https://meta.nxvms.com/download/linux)
  - [Nx Meta Beta Downloads](https://meta.nxvms.com/downloads/patches)
- DW Spectrum:
  - [JSON API](https://dwspectrum.digital-watchdog.com/api/utils/downloads)
  - [DW Spectrum Downloads](https://dwspectrum.digital-watchdog.com/download/linux)
  - The latest DW Spectrum versions are often not listed, but sometimes do match the same version as used by Nx Witness.
  - Use the latest NX Witness URL, and substitute the "default" string for "digitalwatchdog", e.g.:
    - [https://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb](https://updates.networkoptix.com/default/30917/linux/nxwitness-server-4.0.0.30917-linux64.deb)
    - [https://updates.networkoptix.com/digitalwatchdog/30917/linux/dwspectrum-server-4.0.0.30917-linux64.deb](https://updates.networkoptix.com/digitalwatchdog/30917/linux/dwspectrum-server-4.0.0.30917-linux64.deb)

Updating the mediaserver inside docker is not supported, to update the server version pull a new docker container, it is "the docker way".

There is currently no support to automatically detect newly released versions, and then automatically rebuild when a new version is detected. This could possibly be done using page change notifiers and webhooks, with some code to extract the relevant version and URL attributes from the web pages. For now I'm manually updating as I notice the versions changing.

## Build Process

With three products and two base images we end up with six different dockerfiles, that all basically look the same. Unfortunately Docker does [not support](https://github.com/moby/moby/issues/735) an `include` directive, so I [use](http://bobbynorton.com/posts/includes-in-dockerfiles-with-m4-and-make/) a `Makefile` to dynamically create a `Dockerfile` for every variant.

The images are updated weekly using GitHub Actions and picks up the latest upstream changes.

## Network Optix and Docker

### Issues

The biggest outstanding docker challenges are hardware bound licensing, and lack of admin defined storage locations.

The camera license keys are activated using hardware attributes of the server, that is not docker friendly, and occasionally results in activation failures. I would much prefer a modern approach to apply licenses to my cloud account, allowing me to run on whatever hardware I want.

Living in the US, I have to buy my licenses from [Digital Watchdog](https://digital-watchdog.com/), and in my experience their license enforcement policy is inflexible, three activations and you have to buy a new license. That really means that the [Lifetime Upgrades and No Annual Agreements](https://digital-watchdog.com/dws/upgrades/) license is the lifetime of the hardware on which the license was activated. So let's say hardware is replaced every two years, three activations, lifetime is about six years, not much of a lifetime compared to other products that are license flexible but require maintenance or renewals. In my personal opinion commercial software development is not sustainable without periodic support or upgrade fees that covers the ongoing cost of maintenance and support.

As for storage, the mediaserver attempts to automatically decide what storage to use, applies filesystem type and instance filtering, and blindly creates files on any storage it deems fit. This overly complicated logic does not work in docker, and a much simpler and reliable approach would be to allow an admin to be in control and specify exactly what storage to be used. I really do not understand what the reasons were for building complicated decision logic, instead of being like all other servers that use storage and let the admin define the storage locations.

All in, Nx Witness is in my experience the lightest on resources, has good features VMS/NVR, with docker support, and is great to run in my home lab.

### Wishlist

My wishlist for better [docker support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker):

- Publish always up to date and ready to use docker images on Docker Hub.
- Do not bind the license to hardware, use the cloud account for license enforcement.
- Do not filter storage filesystems, allow the administrator to specify and use any storage location.
- Do not bind to any discovered network adapter, allow the administrator to specify the bound network adapter, or an option to opt-out of auto-binding.
- Do not pollute the filesystem by blindly creating folders in any detected storage.
- Implement a [more useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy) recording archive management system, allowing for separate high speed recording, and high capacity playback storage volumes.

## Notes

- I only run NxMeta-LSIO:stable in my home lab, so other images get very little to no testing, please test accordingly.
- Nx Issues:
  - The filesystem filter logic incorrectly considers some volumes to be duplicates (why?), turn on verbose logging (`logLevel=DEBUG2`). `VERBOSE nx::vms::server::fs: shfs /archive fuse.shfs - duplicate`.
  - The mediaserver pollutes the filesystem by blindly creating a `Nx MetaVMS Media` folder and DB files in any storage it finds.
  - The mediaserver will bind to any network adapter it discovers, including virtual adapters used by other containers. There is no way to disable auto binding. All the bound network adapters are displayed in the performance graph, and makes it near impossible to use due to no visible labels.
  - The download CDN SSL certificates are not trusted on all systems, and we need to disable certificate checks when using HTTPS for downloads. `ERROR: cannot verify updates.networkoptix.com's certificate, issued by 'CN=Amazon,OU=Server CA 1B,O=Amazon,C=US': Unable to locally verify the issuer's authority. To connect to updates.networkoptix.com insecurely, use --no-check-certificate`
- Windows Subsystem for Linux v2 (WSL2) is not supported.
  - In the DEB installer `postinst` step the installer tries to start the service, and fails the install. `Detected runtime type: wsl.`, `System has not been booted with systemd as init system (PID 1). Can't operate.`
  - The logic tests for `if [[ $RUNTIME != "docker" ]]`, while the runtime reported by WSL2 is `wsl`.
  - The logic [should](https://support.networkoptix.com/hc/en-us/community/posts/1500000699041-WSL2-docker-runtime-not-supported) perform a `systemd` positive test vs. testing for not docker.
- Version 4.1+ added the ability to specify additional storage filesystem [types](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#notes-about-storage).
  - This is particularly useful because Unraid, ZFS, and Docker Desktop storage is by default not supported (why?).
  - Access the server storage page at `https://hostname:7001/static/index.html#/info` and verify that all mounted storage is listed.
  - If storage is not listed, attach to the container console and run `cat /proc/mounts` to get a list of all the mounted filesystem types.
  - Access the advanced settings page at `https://hostname:7001/static/index.html#/advanced` and set `additionalLocalFsTypes` to include the filesystem type.
  - Add `fuse.grpcfuse` for Docker for Windows, `fuse.shfs` for Unraid, and `zfs` for ZFS, e.g. `fuse.grpcfuse,fuse.shfs,zfs`.
  - Save the settings, restart the server, and verify that storage is now available.
  - Alternatively call the API directly e.g. `wget --user=admin --password=password https://hostname:7001/api/systemSettings?additionalLocalFsTypes=zfs --no-check-certificate`.
  - There is no way to configure the `additionalLocalFsTypes` types at deployment time.
  - Some debugging shows the setting is stored in the `var/ecs.sqlite` DB file, in the `vms_kvpair` table, `name=additionalLocalFsTypes`, `value=fuse.grpcfuse,fuse.shfs,zfs`.
  - This DB table contains lots of other information, so it seems unfeasible to pre-seed the system with this DB file, and modifying it at runtime is as complex as calling the web service.
- Version 5.0:
  - The old shell script `mediaserver` is now what used to be `mediaserver-bin`, and `root-tool` is now what used to be `root-tool-bin`.
  - After upgrading to 5.0, reverting to 4.2 is no longer possible, be sure to make a copy of the server configuration before upgrading. `ERROR ec2::detail::QnDbManager(...): DB Error at ec2::ErrorCode ec2::detail::QnDbManager::doQueryNoLock(...): No query Unable to fetch row`
