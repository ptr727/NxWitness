# Docker Projects for Nx Witness and Nx Meta and DW Spectrum

This is a project to build docker containers for [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/), and [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness, and [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

## License

Licensed under the [MIT License](./LICENSE).  
![GitHub License](https://img.shields.io/github/license/ptr727/NxWitness)

## Build Status

[Code and Pipeline is on GitHub](https://github.com/ptr727/NxWitness):  
[![GitHub Last Commit](https://img.shields.io/github/last-commit/ptr727/NxWitness?logo=github)](https://github.com/ptr727/NxWitness/commits/main)  
[![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/ptr727/NxWitness/BuildPublishPipeline.yml?branch=main&logo=github)](https://github.com/ptr727/NxWitness/actions)  
[![GitHub Actions Last Build](https://byob.yarr.is/ptr727/NxWitness/lastbuild)](https://github.com/ptr727/NxWitness/actions)

## Build Issues

- Automatic updating of Nx build versions during scheduled builds are disabled, see this [issue](https://github.com/ptr727/NxWitness/issues/73) for details.
- Publishing of `stable` tags are disabled, see this [issue](https://github.com/ptr727/NxWitness/issues/62) for details.
- Publishing to GHCR was removed, it is just not reliable and kept breaking the builds with `failed to push ghcr.io ... write: broken pipe` errors.

## Releases

Docker container images are published on [Docker Hub](https://hub.docker.com/u/ptr727).  
Images are tagged using `latest`, `stable`, and the specific version number.  
The `latest` tag uses the latest release version, `latest` may be the same as `rc` or `beta`.  
The `stable` tag uses the stable release version, `stable` may be the same as `latest`.  
The `develop`, `rc` or `beta` tags are assigned to test or pre-release versions, they are not generated with every release, use with care and only as needed.

E.g.

```console
# Latest NxMeta-LSIO from Docker
docker pull ptr727/nxmeta-lsio:latest
# Stable DWSpectrum from GitHub
docker pull ptr727/dwspectrum:stable
# 5.0.0.35136 NxWitness-LSIO from Docker
docker pull ptr727/nxwitness-lsio:5.0.0.35136
```

The images are updated weekly, picking up the latest upstream OS updates, and newly released product versions.  
See the [Build Process](#build-process) section for more details on how versions and builds are managed.

[NxWitness](https://hub.docker.com/r/ptr727/nxwitness)  
[![NxWitness Stable](https://img.shields.io/docker/v/ptr727/nxwitness/stable?label=stable&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness)
[![NxWitness Latest](https://img.shields.io/docker/v/ptr727/nxwitness/latest?label=latest&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness)
[![NxWitness RC](https://img.shields.io/docker/v/ptr727/nxwitness/rc?label=rc&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness)
[![NxWitness Beta](https://img.shields.io/docker/v/ptr727/nxwitness/beta?label=beta&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness)

[NxWitness-LSIO](https://hub.docker.com/r/ptr727/nxwitness-lsio)  
[![NxWitness-LSIO Stable](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/stable?label=stable&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness-lsio)
[![NxWitness-LSIO Latest](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/latest?label=latest&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness-lsio)
[![NxWitness-LSIO RC](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/rc?label=rc&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness-lsio)
[![NxWitness-LSIO Beta](https://img.shields.io/docker/v/ptr727/nxwitness-lsio/beta?label=beta&logo=docker)](https://hub.docker.com/r/ptr727/nxwitness-lsio)

[NxMeta](https://hub.docker.com/r/ptr727/nxmeta)  
[![NxMeta Stable](https://img.shields.io/docker/v/ptr727/nxmeta/stable?label=stable&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta)
[![NxMeta Latest](https://img.shields.io/docker/v/ptr727/nxmeta/latest?label=latest&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta)
[![NxMeta RC](https://img.shields.io/docker/v/ptr727/nxmeta/rc?label=rc&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta)
[![NxMeta Beta](https://img.shields.io/docker/v/ptr727/nxmeta/beta?label=beta&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta)

[NxMeta-LSIO](https://hub.docker.com/r/ptr727/nxmeta-lsio)  
[![NxMeta-LSIO Stable](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/stable?label=stable&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta-lsio)
[![NxMeta-LSIO Latest](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/latest?label=latest&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta-lsio)
[![NxMeta-LSIO RC](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/rc?label=rc&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta-lsio)
[![NxMeta-LSIO Beta](https://img.shields.io/docker/v/ptr727/nxmeta-lsio/beta?label=beta&logo=docker)](https://hub.docker.com/r/ptr727/nxmeta-lsio)

[DWSpectrum](https://hub.docker.com/r/ptr727/dwspectrum)  
[![DWSpectrum Stable](https://img.shields.io/docker/v/ptr727/dwspectrum/stable?label=stable&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum)
[![DWSpectrum Latest](https://img.shields.io/docker/v/ptr727/dwspectrum/latest?label=latest&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum)
[![DWSpectrum RC](https://img.shields.io/docker/v/ptr727/dwspectrum/rc?label=rc&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum)
[![DWSpectrum Beta](https://img.shields.io/docker/v/ptr727/dwspectrum/beta?label=beta&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum)

[DWSpectrum-LSIO](https://hub.docker.com/r/ptr727/dwspectrum-lsio)  
[![DWSpectrum-LSIO Stable](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/stable?label=stable&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum-lsio)
[![DWSpectrum-LSIO Latest](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/latest?label=latest&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum-lsio)
[![DWSpectrum-LSIO RC](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/rc?label=rc&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum-lsio)
[![DWSpectrum-LSIO Beta](https://img.shields.io/docker/v/ptr727/dwspectrum-lsio/beta?label=beta&logo=docker)](https://hub.docker.com/r/ptr727/dwspectrum-lsio)

## Overview

### Introduction

I ran DW Spectrum in my home lab on an Ubuntu Virtual Machine, and was looking for a way to run it in Docker. Nx Witness provided no support for Docker, but I did find the [The Home Repot NxWitness](https://github.com/thehomerepot/nxwitness) project, that inspired me to create this project.  
I started with individual repositories for Nx Witness, Nx Meta, and DW Spectrum, but that soon became cumbersome with lots of duplication, and I combined all product flavors into this one project.

As of recent Network Optix does provide [Experimental Docker Support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker), and they publish a [reference docker project](https://github.com/networkoptix/nx_open_integrations/tree/master/docker), but they do not publish container images.  

The biggest outstanding challenges with running in docker are hardware bound licensing and lack of admin defined storage locations, see the [Network Optix and Docker](#network-optix-and-docker) section for details.  

### Products

The project supports three product variants:

- [Network Optix Nx Witness VMS](https://www.networkoptix.com/nx-witness/).
- [Network Optix Nx Meta VMS](https://meta.nxvms.com/), the developer test and preview version of Nx Witness.
- [Digital Watchdog DW Spectrum IPVMS](https://digital-watchdog.com/productdetail/DW-Spectrum-IPVMS/), the US licensed and OEM branded version of Nx Witness.

### Base Images

The project creates two variants of each product using different base images:

- [Ubuntu](https://ubuntu.com/) using [ubuntu:focal](https://hub.docker.com/_/ubuntu) base image.
- [LinuxServer](https://www.linuxserver.io/) using [lsiobase/ubuntu:focal](https://hub.docker.com/r/lsiobase/ubuntu) base image.

Note that smaller base images, like [Alpine](https://alpinelinux.org/), and the current [Ubuntu 22.04 LTS (Jammy Jellyfish)](https://releases.ubuntu.com/22.04/) are not [supported](https://support.networkoptix.com/hc/en-us/articles/205313168-Nx-Witness-Operating-System-Support) by the mediaserver.

### LinuxServer

The [LinuxServer (LSIO)](https://www.linuxserver.io/) base images provide valuable functionality:

- The LSIO images are based on [s6-overlay](https://github.com/just-containers/s6-overlay), and LSIO [produces](https://fleet.linuxserver.io/) containers for many popular open source applications.
- LSIO allows us to [specify](https://docs.linuxserver.io/general/understanding-puid-and-pgid) the user account to use when running the container mediaserver process.
- This is [desired](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/#user) if we do not want to run as root, or required if we need user specific permissions when accessing mapped volumes.
- We could achieve a similar outcome by using Docker's [`--user`](https://docs.docker.com/engine/reference/run/#user) option, but the mediaserver's `root-tool` (used for license enforcement) requires running as `root`, thus the container must still be executed with `root` privileges, and we cannot use the `--user` option.
- The non-LSIO images do run the mediaserver as a non-root user, granting `sudo` rights to run the `root-tool` as `root`, but the user account `${COMPANY_NAME}` does not readily map to a user on the host system.

## Configuration

The docker configuration is simple, requiring just two volume mappings for configuration files and media storage.

### Volumes

`/config` : Configuration files.  
`/media` : Recording files.  
`/archive` : Backup files. (Optional)

Note that if your storage is not showing up, see the [Missing Storage](#missing-storage) section for help.

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
- Register the Unraid filesystems in the `additionalLocalFsTypes` advanced settings, see the [Missing Storage](#missing-storage) section for help.

## Product Information

### Releases Information

- Nx Witness:
  - [Downloads API](https://nxvms.com/api/utils/downloads)
  - [Releases API](https://updates.vmsproxy.com/default/releases.json)
  - [Downloads](https://nxvms.com/download/linux)
  - [Beta Downloads](https://beta.networkoptix.com/beta-builds/default)
  - [Release Notes](https://www.networkoptix.com/all-nx-witness-release-notes)
- Nx Meta:
  - [Downloads API](https://meta.nxvms.com/api/utils/downloads)
  - [Releases API](https://updates.vmsproxy.com/metavms/releases.json)
  - [Early Access Signup](https://support.networkoptix.com/hc/en-us/articles/360046713714-Get-an-Nx-Meta-Build)
  - [Request Developer Licenses](https://support.networkoptix.com/hc/en-us/articles/360045718294-Getting-Licenses-for-Developers)
  - [Downloads](https://meta.nxvms.com/download/linux)
  - [Beta Downloads](https://meta.nxvms.com/downloads/patches)
- DW Spectrum:
  - [Downloads API](https://dwspectrum.digital-watchdog.com/api/utils/downloads)
  - [Releases API](https://updates.vmsproxy.com/digitalwatchdog/releases.json)
  - [Downloads](https://dwspectrum.digital-watchdog.com/download/linux)
  - [Release Notes](https://digital-watchdog.com/DWSpectrum-Releasenote/DWSpectrum.html)

### Advanced Configuration

- Advanced `mediaserver.conf` [Configuration](https://support.networkoptix.com/hc/en-us/articles/360036389693-How-to-access-Nx-Server-configuration-options):
  - v4: `https://[hostname]:7001/static/index.html#/developers/serverDocumentation`
  - v5: JSON: `https://[hostname]:7001/api/settingsDocumentation`
- Advanced Web Configuration:
  - v4: `https://[hostname]:7001/static/index.html#/advanced`
  - v5: `https://[hostname]:7001/#/settings/advanced`
  - Get State: JSON: `https://[hostname]:7001/api/systemSettings`
- Storage Reporting:
  - v4: `https://[hostname]:7001/static/health.html#/health/storages`
  - v5: `https://[hostname]:7001/#/health/storages`

## Build Process

The build is divided into the following parts:

- A [Makefile](./Make/Makefile) is used to create the `Dockerfile`'s for permutations of "Entrypoint" and "LSIO" variants, and for each of "NxMeta", "NxWitness" and "DWSpectrum" products.
  - There is similarity between the container variants, and to avoid code duplication the `Dockerfile` is dynamically constructed using file snippets.
  - Docker does [not support](https://github.com/moby/moby/issues/735) a native `include` directive, instead the [M4 macro processor](https://www.gnu.org/software/m4/) is used to assemble the snippets.
  - The various docker project directories are created by running `make create`.
  - The project directories could be created at build time, but they are currently created and checked into source control to simplify change review.
- The `Dockerfile` downloads and installs the mediaserver installer at build time using the `DOWNLOAD_URL` environment variable.
  - The Nx download URL can be a DEB file or a ZIP file containing a DEB file, and the DEB file in the ZIP file may not be the same name as the ZIP file.
  - The [Download.sh](./Make/Download.sh) script handles the variances making the DEB file available to install.
  - It is possible to download the DEB file outside the `Dockerfile` and `COPY` it into the image, but the current process downloads inside the `Dockerfile` to minimize external build dependencies.
- Updating the available product versions and download URL's are done using the custom [CreateMatrix](./CreateMatrix/) utility app.
  - Version information can be manually curated in the [Version.json](./Make/Version.json) file.
  - Version information can also be constructed online using the mediaserver [release API](https://updates.vmsproxy.com/default/releases.json), using the same logic as in the [Nx Open](https://github.com/networkoptix/nx_open/blob/master/vms/libs/nx_vms_update/src/nx/vms/update/releases_info.cpp) desktop client.
  - The `CreateMatrix` app can construct a [Matrix.json](./Make/Matrix.json) file from the `Version.json` file or from online version information.
    - `CreateMatrix matrix --version=./Make/Version.json --matrix=./Make/Matrix.json`
    - `CreateMatrix matrix --matrix=./Make/Matrix.json --online=true`
- Local builds can be performed using `make build`, where download URL and version information defaults to the `Dockerfile` values.
  - All images will be built and launched using `make build` and `make up`, allowing local testing using the build output URL's.
  - After testing stop and delete containers and images using `make clean`.
- Automated builds are done using [GitHub Actions](https://docs.github.com/en/actions) and the [BuildPublishPipeline.yml](./.github/workflows/BuildPublishPipeline.yml) pipeline.
  - The pipeline runs the `CreateMatrix` utility to create a `Matrix.json` file containing all the container image details.
  - A [Matrix](https://docs.github.com/en/actions/using-jobs/using-a-matrix-for-your-jobs) strategy is used to build and publish a container image for every entry in the `Matrix.json` file.
  - Conditional build time branch logic controls image creation vs. image publishing.
- Updating the mediaserver inside docker is not supported, to update the server version pull a new container image, it is "the docker way".

## Network Optix and Docker

There are issues ranging from annoyances to serious with Network Optix on Docker, but compared to other VMS/NVR software I've paid for and used, it is very light on system resources, has a good feature set, and with added docker support runs great in my home lab.

### Licensing

**Issue:**  
The camera recording license keys are activated and bound to hardware attributes of the host server.  
Docker containers are supposed to be portable, and moving containers between hosts will break license activation.

**Possible Solution:**  
A portable approach could apply licenses to the [Cloud Account](https://www.networkoptix.com/nx-witness/nx-witness-cloud/), allowing runtime enforcement that is not hardware bound.

### Storage Management

**Issue:**  
The mediaserver attempts to automatically decide what storage to use.  
Filesystem types are filtered out if not on the [supported list](https://github.com/networkoptix/nx_open_integrations/tree/master/docker#notes-about-storage), e.g. popular and common [ZFS](https://support.networkoptix.com/hc/en-us/community/posts/1500000914242-Please-add-ZFS-as-supported-storage-to-4-3) is not supported.  
Duplicate filesystems are ignored, e.g. multiple logical mounts on the same physical storage are ignored.  
The server blindly creates database files on any writable storage it discovers, regardless of if that storage was assigned for use or not.

**Possible Solution:**  
Remove the elaborate and prone to failure filesystem discovery and filtering logic, use the specified storage, and only the specified storage.

### Network Binding

**Issue:**  
The mediaserver binds to [any discovered](https://support.networkoptix.com/hc/en-us/community/posts/360048795073-R8-in-Docker-auto-binds-to-any-network-adapter-it-finds) network adapter.
On docker this means the server binds to all docker networks of all running containers, there could be hundreds or thousands, making the network graph useless, and consuming unnecessary resources.

**Possible Solution:**  
Remove the auto-bind functionality, or make it configurable with the default disabled, and allow the administrator to define the specific networks to bind with.

### Lifetime Upgrades

**Issue:**  
This section is personal opinion, I've worked in the ISV industry for many years, and I've taken perpetually licensed products to SaaS.

Living in the US, I have to buy my licenses from [Digital Watchdog](https://digital-watchdog.com/), and in my experience their license enforcement policy is inflexible, three activations and you have to buy a new license.  
That really means that the [Lifetime Upgrades and No Annual Agreements](https://dwspectrum.com/upgrades/) license is the lifetime of the hardware on which the license was activated. So let's say hardware is replaced every two years, three activations, lifetime is about six years, not much of a lifetime compared to my lifetime.  

There is no such thing as free of cost software, at minimum somebody pays for time, and at minimum vulnerabilities must be fixed, the EULA does not excuse an ISV from willful neglect.  
Add in ongoing costs of cloud hosting, cost of development of new features, and providing support, where does the money come from?  
Will we eventually see a license scheme change, or is it a customer [acquisition](https://www.crunchbase.com/organization/network-optix) and sell or go public play, but hopefully not a cash out and bail scheme?

**Possible Solution:**  
I'd be happy to pay a reasonable yearly subscription or maintenance fee, knowing I get ongoing fixes, features, and support, and my licenses being tied to my cloud account.

### Wishlist

My wishlist for better docker support:

- Publish always up to date and ready to use docker images on Docker Hub.
- Do not bind the license to hardware, use the cloud account for license enforcement.
- Do not filter storage filesystems, allow the administrator to specify and use any storage location backed by any filesystem.
- Do not pollute the filesystem by creating folders in any detected storage, use only storage as specified.
- Do not bind to any discovered network adapter, allow the administrator to specify the bound network adapter, or add an option to opt-out/opt-in to auto-binding.
- Implement a [more useful](https://support.networkoptix.com/hc/en-us/community/posts/360044221713-Backup-retention-policy) recording archive management system, allowing for separate high speed recording, and high capacity playback storage volumes. E.g. as implemented by [Milestone XProtect VMS](https://doc.milestonesys.com/latest/en-US/standard_features/sf_mc/sf_systemoverview/mc_storageandarchivingexplained.htm).

Please do [contact](https://support.networkoptix.com/hc/en-us/community/topics) Network Optix and ask for better [docker support](https://support.networkoptix.com/hc/en-us/articles/360037973573-How-to-run-Nx-Server-in-Docker).

## Troubleshooting

I am not affiliated with Network Optix, I cannot provide support for their products, please contact [Network Optix Support](https://support.networkoptix.com/hc/en-us/community/topics) for product support issues.  
If there are issues with the docker build scripts used in this project, please create a [GitHub Issue](https://github.com/ptr727/NxWitness/issues).  
Note that I only test and run `nxmeta-lsio:stable` in my home lab, other images get very little to no testing, please test accordingly.

### Known Issues

- v4 does not support Windows Subsystem for Linux v2 (WSL2).
  - The DEB installer `postinst` step tries to start the service, and fails the install.
    - `Detected runtime type: wsl.`
    - `System has not been booted with systemd as init system (PID 1). Can't operate.`
  - v4 logic tests for `if [[ $RUNTIME != "docker" ]]`, while the runtime reported by WSL2 is `wsl` not `docker`.
  - v5 logic tests for `if [[ -f "/.dockerenv" ]]`, the presence of a Docker environment, that is more portable, and does work in WSL2.
- Downgrading from v5 to v4 is not supported.
  - The mediaserver will fail to start.
    - `ERROR ec2::detail::QnDbManager(...): DB Error at ec2::ErrorCode ec2::detail::QnDbManager::doQueryNoLock(...): No query Unable to fetch row`
  - Make a copy, or ZFS snapshot, of the server configuration before upgrading, and restore the old configuration when downgrading.

### Missing Storage

The following section will help troubleshoot common problems with missing storage.  
If this does not help, please contact [Network Optix Support](https://support.networkoptix.com/hc/en-us/community/topics).  
Please do not open a GitHub issue unless you are positive the issue is with the `Dockerfile`.

Note that the configuration URL's changed between v4 and v5, see the [Advanced Configuration](#advanced-configuration) section for version specific URL's.

Confirm that all the mounted volumes are listed in the available storage locations in the [web admin](https://support.networkoptix.com/hc/en-us/articles/115012831028-Nx-Server-Web-Admin) portal.

Enable debug logging in the mediaserver:  
Edit `/config/etc/mediaserver.conf`, set `logLevel=verbose`, restart the server.  
Look for clues in `/config/var/log/log_file.log`.

E.g.

```log
VERBOSE nx::vms::server::fs: shfs /media fuse.shfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /media btrfs - duplicate
DEBUG QnStorageSpaceRestHandler(0x7f85043b0b00): Return 0 storages and 1 protocols
```

Get a list of the mapped volume mounts in the running container, and verify that `/config` and `/media` is in the JSON `Mounts` section:

```console
docker ps --no-trunc
docker container inspect [containername]
```

Launch a shell in the running container and get a list of filesystems mounts:

```console
docker ps --no-trunc
docker exec --interactive --tty [containername] /bin/bash
cat /proc/mounts
exit
```

Example output for ZFS:

```console
ssdpool/appdata /config zfs rw,noatime,xattr,posixacl 0 0
nvrpool/nvr /media zfs rw,noatime,xattr,posixacl 0 0
ssdpool/docker /archive zfs rw,noatime,xattr,posixacl 0 0
```

Mount `/config` is on device `ssdpool/appdata` and filesystem is `zfs`.  
Mount `/media` is on device `nvrpool/nvr` and filesystem is `zfs`.  
Mount `/archive` is on device `ssdpool/docker` and filesystem is `zfs`.

In this case the devices are unique and will not be filtered, but `zfs` is not supported and needs to be registered.

Example output for UnRaid FUSE:

```console
shfs /config fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0
shfs /media fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0
shfs /archive fuse.shfs rw,nosuid,nodev,noatime,user_id=0,group_id=0,allow_other 0 0
```

In this case there are two issues, the device is `/shfs` for all three mounts and will be filtered, and the filesystem type is `fuse.shfs` that is not supported and needs to be registered.

Log file output for Unraid FUSE:

```log
VERBOSE nx::vms::server::fs: shfs /config fuse.shfs - added
VERBOSE nx::vms::server::fs: shfs /media fuse.shfs - added
VERBOSE nx::vms::server::fs: shfs /archive fuse.shfs - duplicate
```

The `/archive` mount is classified as a duplicate and ignored, map just `/media`, do not map `/archive`.  
Alternative use the "Unassigned Devices" plugin and dedicate e.g. a XFS formatted SSD drive to `/media` and/or `/config`.

Example output for Unraid BTRFS:

```console
/dev/sdb8 /test btrfs rw,relatime,space_cache,subvolid=5,subvol=/test 0 0
/dev/sdb8 /config btrfs rw,relatime,space_cache,subvolid=5,subvol=/config 0 0
/dev/sdb8 /media btrfs rw,relatime,space_cache,subvolid=5,subvol=/media 0 0
/dev/sdb8 /archive btrfs rw,relatime,space_cache,subvolid=5,subvol=/archive 0 0
```

```log
VERBOSE nx::vms::server::fs: /dev/sdb8 /test btrfs - added
VERBOSE nx::vms::server::fs: /dev/sdb8 /config btrfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /media btrfs - duplicate
VERBOSE nx::vms::server::fs: /dev/sdb8 /archive btrfs - duplicate
```

In this example the `/test` volume was accepted, but all other volumes on `/dev/sdb8` was ignored as duplicates.

Add the required filesystem types in the [advanced configuration](#advanced-configuration) menu.
Edit the `additionalLocalFsTypes` option and add the required filesystem types, e.g. `fuse.shfs,btrfs,zfs`, restart the server.

Alternatively call the configuration API directly:  
`wget --no-check-certificate --user=[username] --password=[password] https://[hostname]:7001/api/systemSettings?additionalLocalFsTypes=fuse.shfs,btrfs,zfs`.

To my knowledge there is no solution to duplicate devices being filtered, please contact [Network Optix Support](https://support.networkoptix.com/hc/en-us/community/topics) and ask them to stop filtering filesystem types and devices.
